namespace GlassBridge.Internal;

using GlassBridge.Internal.HID;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

/// <summary>
/// VITURE系グラス用IMUデバイス実装
/// IMU/MCUストリームの判別はこのクラスが責務（VITURE固有のドメイン知識）
/// </summary>
internal sealed class VitureLumaDevice : IImuDevice
{
    private static readonly ILogger<VitureLumaDevice> _logger = LoggerFactoryProvider.Instance.CreateLogger<VitureLumaDevice>();

    internal const int VendorId = 0x35CA;

    internal static readonly int[] SupportedProductIds =
    [
        0x1011, 0x1013, 0x1017,  // VITURE One
        0x1015, 0x101b,           // VITURE One Lite
        0x1019, 0x101d,           // VITURE Pro
        0x1121, 0x1141,           // VITURE Luma Pro
        0x1131                    // VITURE Luma
    ];

    private readonly IHidStreamProvider _hidProvider;

    // VITURE固有：IMU/MCUストリーム（ドメイン知識）
    private IHidStream? _imuStream;
    private IHidStream? _mcuStream;

    private bool _isConnected;
    private bool _disposed;
    private ushort _messageCounter;

    public bool IsConnected => _isConnected && !_disposed;

    private VitureLumaDevice(IHidStreamProvider hidProvider)
    {
        _hidProvider = hidProvider ?? throw new ArgumentNullException(nameof(hidProvider));
        _messageCounter = 0;
    }

    /// <summary>
    /// デバイスに接続し、IMU有効化コマンドを送信
    /// </summary>
    public static async Task<VitureLumaDevice?> ConnectAsync(CancellationToken cancellationToken = default)
    {
        // HidSharpの汎用ラッパーを使用
        var provider = new HidStreamProvider();
        return await ConnectWithProviderAsync(provider, cancellationToken);
    }

    /// <summary>
    /// 指定されたプロバイダでデバイスを初期化（テスト用）
    /// </summary>
    internal static async Task<VitureLumaDevice?> ConnectWithProviderAsync(
        IHidStreamProvider hidProvider,
        CancellationToken cancellationToken = default)
    {
        var device = new VitureLumaDevice(hidProvider);

        if (await device.InitializeAsync(cancellationToken))
            return device;

        await device.DisposeAsync();
        return null;
    }

    /// <summary>
    /// デバイスを初期化
    /// IMU/MCUストリームの判別を行う（VITURE固有ロジック）
    /// </summary>
    private async Task<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Device initialization started");

            // HidStreamProviderから全ストリームを取得
            var allStreams = await _hidProvider.GetStreamsAsync(
                VendorId,
                SupportedProductIds,
                cancellationToken);
            if (allStreams.Count < 2)
            {
                _logger.LogError("Expected at least 2 streams, but found {StreamCount}", allStreams.Count);
                return false;
            }

            _logger.LogDebug("Found {StreamCount} streams, identifying IMU and MCU...", allStreams.Count);

            // VITURE固有：IMU/MCUを判別
            await IdentifyStreamsAsync(allStreams, cancellationToken);

            if (_imuStream == null || _mcuStream == null)
            {
                _logger.LogError("Stream identification failed: IMU={ImuStreamOk}, MCU={McuStreamOk}", _imuStream != null, _mcuStream != null);
                return false;
            }

            _logger.LogInformation("Stream identification successful: IMU and MCU identified");

            // ストリーム判別後、IMUを無効化する
            // GetImuDataStreamAsync 呼び出し時にだけ有効化することで、
            // 古いデータがUSBバッファに蓄積されることを防ぐ
            try
            {
                await SendImuEnableCommandAsync(enable: false, cancellationToken);
            }
            catch
            {
                // 初期化処理の一部なので、失敗してもシステムは動作継続
            }

            _isConnected = true;
            _logger.LogDebug("Device initialization completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// VITURE固有：ストリームからIMU/MCUを判別
    /// 有効なコマンドパケットを送信して応答をテスト
    /// ドキュメント参照：「送信可否でコマンドの宛先を事実上判別している」
    /// </summary>
    private async Task IdentifyStreamsAsync(IReadOnlyList<IHidStream> streams, CancellationToken cancellationToken)
    {
        // シンプルな判別：最初のストリームを MCU、2番目を IMU とする
        // （実装は WebHID の判別方式に基づく）
        _logger.LogDebug("Identifying IMU and MCU streams from {StreamCount} available streams", streams.Count);

        for (int i = 0; i < streams.Count; i++)
        {
            var stream = streams[i];
            _logger.LogDebug("Testing stream #{StreamIndex} for identification", i);

            try
            {
                // 有効な IMU enable コマンドパケットを送信
                var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable: true, messageCounter: 0);
                
                // デバイスの MaxOutputReportLength に基づいてバッファを作成
                var writeBuffer = new byte[stream.MaxOutputReportLength];
                writeBuffer[0] = 0x00; // Report ID
                Array.Copy(cmdPacket, 0, writeBuffer, 1, Math.Min(cmdPacket.Length, writeBuffer.Length - 1));

                _logger.LogTrace("Sending IMU enable command to stream #{StreamIndex}, packet size: {PacketSize}", i, cmdPacket.Length);
                await stream.WriteAsync(writeBuffer, cancellationToken);

                // 応答待機（タイムアウト付き）
                // デバイスの MaxInputReportLength に基づいてバッファを作成
                var ackBuffer = new byte[stream.MaxInputReportLength];
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMilliseconds(100));

                try
                {
                    int bytesRead = await stream.ReadAsync(ackBuffer, 0, ackBuffer.Length, cts.Token);

                    // Report ID オフセットを検出
                    int offset = (bytesRead > 1 && ackBuffer[0] == 0x00 && ackBuffer[1] == 0xFF) ? 1 : 0;

                    // 応答があればこのストリームが MCU
                    if (bytesRead > offset && ackBuffer[offset] == 0xFF)
                    {
                        // MCU ACK か IMU データか確認
                        if (ackBuffer[offset + 1] == 0xFD)
                        {
                            _mcuStream = stream;
                            _logger.LogInformation("Stream #{StreamIndex} identified as MCU (ACK received: 0xFF 0xFD)", i);
                            continue;
                        }
                        else if (ackBuffer[offset + 1] == 0xFC)
                        {
                            // IMU データが返ってきた
                            _imuStream = stream;
                            _logger.LogInformation("Stream #{StreamIndex} identified as IMU (data received: 0xFF 0xFC)", i);
                            continue;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // タイムアウト → IMU
                    if (_imuStream == null)
                    {
                        _imuStream = stream;
                        _logger.LogDebug("Stream #{StreamIndex} identified as IMU (timeout on ACK wait)", i);
                    }
                    continue;
                }
            }
            catch (Exception ex)
            {
                // エラー → IMU
                if (_imuStream == null)
                {
                    _imuStream = stream;
                    _logger.LogDebug(ex, "Stream #{StreamIndex} identified as IMU (exception on write): {ErrorMessage}", i, ex.Message);
                }
            }
        }

        // 未割り当てのストリームを残りに割り当てる
        if (_mcuStream == null && _imuStream != null)
        {
            for (int i = 0; i < streams.Count; i++)
            {
                if (streams[i] != _imuStream)
                {
                    _mcuStream = streams[i];
                    _logger.LogDebug("MCU stream assigned to stream #{StreamIndex} (fallback)", i);
                    break;
                }
            }
        }
        else if (_imuStream == null && _mcuStream != null)
        {
            for (int i = 0; i < streams.Count; i++)
            {
                if (streams[i] != _mcuStream)
                {
                    _imuStream = streams[i];
                    _logger.LogDebug("IMU stream assigned to stream #{StreamIndex} (fallback)", i);
                    break;
                }
            }
        }

        _logger.LogInformation("Stream identification complete: IMU={ImuStreamOk}, MCU={McuStreamOk}", _imuStream != null, _mcuStream != null);
    }

    /// <summary>
    /// IMUデータストリームを取得
    /// このメソッド呼び出し時にIMUを有効化し、終了時に無効化する
    /// これにより、古いデータがUSBバッファに蓄積されるのを防ぎ、
    /// 呼び出し時点での最新データを取得できる
    /// </summary>
    public async IAsyncEnumerable<ImuData> GetImuDataStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _imuStream == null)
            throw new InvalidOperationException("Device is not connected");

        _logger.LogInformation("IMU data stream started");
        int frameCount = 0;

        // IMU有効化（ストリーム開始時）
        try
        {
            await SendImuEnableCommandAsync(enable: true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable IMU: {ErrorMessage}", ex.Message);
            throw;
        }

        try
        {
            // デバイスの MaxInputReportLength に基づいてバッファを作成
            // VITURE仕様: Report ID (1 byte) + Report Size (64 bytes) = 65 bytes
            var buffer = new byte[_imuStream.MaxInputReportLength];

            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                var imuData = await TryReadImuDataAsync(_imuStream, buffer, cancellationToken);

                if (imuData != null)
                {
                    frameCount++;
                    if (frameCount % 1000 == 0)
                    {
                        _logger.LogDebug("Streamed {FrameCount} IMU data frames", frameCount);
                    }
                    yield return imuData;
                }
                else
                {
                    await Task.Delay(1, cancellationToken);
                }
            }
        }
        finally
        {
            // IMU無効化（ストリーム終了時 - 例外時も必ず実行）
            try
            {
                await SendImuEnableCommandAsync(enable: false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable IMU: {ErrorMessage}", ex.Message);
                // 無効化失敗は致命的ではないため、例外を吐かない
            }

            _logger.LogInformation("IMU data stream ended after {FrameCount} frames", frameCount);
        }
    }

    /// <summary>
    /// HIDストリームからIMUデータを読み込もうとする（非同期）
    /// </summary>
    private async Task<ImuData?> TryReadImuDataAsync(IHidStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        try
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (bytesRead > 0)
            {
                _logger.LogTrace("Read {BytesCount} bytes from IMU stream", bytesRead);

                if (VitureLumaPacket.TryParseImuPacket(buffer.AsSpan(0, bytesRead), out var imuData, skipCrcValidation: true) && imuData != null)
                {
                    _logger.LogTrace("Successfully parsed IMU packet: Counter={MessageCounter}, Timestamp={Timestamp}", 
                        imuData.MessageCounter, imuData.Timestamp);
                    return imuData;
                }
                else
                {
                    _logger.LogDebug("Failed to parse IMU packet from {BytesCount} bytes", bytesRead);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルは正常な終了
            _logger.LogDebug("IMU read cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from IMU stream: {ErrorMessage}", ex.Message);
        }

        return null;
    }

    /// <summary>
    /// IMU有効化/無効化コマンドを送信
    /// </summary>
    private async Task SendImuEnableCommandAsync(bool enable, CancellationToken cancellationToken = default)
    {
        if (_mcuStream == null)
        {
            _logger.LogWarning("MCU stream is null, cannot send IMU {EnableState} command", enable ? "enable" : "disable");
            return;
        }

        var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable, _messageCounter++);

        // デバイスの MaxOutputReportLength に基づいてバッファを作成
        var writeBuffer = new byte[_mcuStream.MaxOutputReportLength];
        writeBuffer[0] = 0x00; // Report ID
        Array.Copy(cmdPacket, 0, writeBuffer, 1, Math.Min(cmdPacket.Length, writeBuffer.Length - 1));

        _logger.LogDebug("Sending IMU {EnableState} command, MessageCounter={MessageCounter}, PacketSize={PacketSize}", 
            enable ? "enable" : "disable", _messageCounter - 1, cmdPacket.Length);

        try
        {
            // MCUストリーム「のみ」に送信
            await _mcuStream.WriteAsync(writeBuffer, cancellationToken);
            _logger.LogTrace("IMU {EnableState} command sent to MCU", enable ? "enable" : "disable");

            // ACK受信待機（タイムアウト付き）
            // デバイスの MaxInputReportLength に基づいてバッファを作成
            var ackBuffer = new byte[_mcuStream.MaxInputReportLength];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(500));

            try
            {
                int bytesRead = await _mcuStream.ReadAsync(ackBuffer, 0, ackBuffer.Length, cts.Token);

                // Report ID オフセットを検出
                int offset = (bytesRead > 1 && ackBuffer[0] == 0x00 && ackBuffer[1] == 0xFF) ? 1 : 0;

                if (bytesRead >= offset + 2)
                {
                    _logger.LogTrace("MCU response: {ResponseByte0:X2} {ResponseByte1:X2}", ackBuffer[offset], ackBuffer[offset + 1]);
                    
                    if (ackBuffer[offset] == 0xFF && ackBuffer[offset + 1] == 0xFD)
                    {
                        _logger.LogDebug("Received MCU ACK for IMU {EnableState} command", enable ? "enable" : "disable");
                    }
                }
                else
                {
                    _logger.LogDebug("MCU response received but invalid length: {BytesCount}", bytesRead);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MCU ACK timeout (acceptable in some cases)");
            }

            await Task.Delay(100, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IMU command: {ErrorMessage}", ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _logger.LogDebug("Disposing VitureLumaDevice");

        if (_isConnected && _mcuStream != null)
        {
            try
            {
                // IMU無効化コマンドを送信
                await SendImuEnableCommandAsync(enable: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disabling IMU during dispose: {ErrorMessage}", ex.Message);
            }
        }

        await _hidProvider.DisposeAsync();

        _isConnected = false;
        _disposed = true;
        
        _logger.LogInformation("VitureLumaDevice disposed");
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}


