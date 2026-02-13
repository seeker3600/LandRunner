namespace GlassBridge.Internal.VitureLuma;

using GlassBridge.Internal.HID;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;
using GlassBridge;

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
            // HidStreamProviderから全ストリームを取得
            var allStreams = await _hidProvider.GetStreamsAsync(
                VendorId,
                SupportedProductIds,
                cancellationToken);
            if (allStreams.Count < 2)
            {
                _logger.LogWarning("必要なストリームが見つかりません（検出: {StreamCount}）", allStreams.Count);
                return false;
            }

            // VITURE固有：IMU/MCUを判別
            await IdentifyStreamsAsync(allStreams, cancellationToken);

            if (_imuStream == null || _mcuStream == null)
            {
                _logger.LogWarning("ストリームの判別に失敗しました");
                return false;
            }

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
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "デバイスの初期化に失敗しました");
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

        for (int i = 0; i < streams.Count; i++)
        {
            var stream = streams[i];

            try
            {
                // 有効な IMU enable コマンドパケットを送信
                var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable: true, messageCounter: 0);
                
                // デバイスの MaxOutputReportLength に基づいてバッファを作成
                var writeBuffer = new byte[stream.MaxOutputReportLength];
                writeBuffer[0] = 0x00; // Report ID
                Array.Copy(cmdPacket, 0, writeBuffer, 1, Math.Min(cmdPacket.Length, writeBuffer.Length - 1));

                await stream.WriteAsync(writeBuffer, cancellationToken);

                // 応答待機（タイムアウト付き）
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
                        if (ackBuffer[offset + 1] == 0xFD)
                        {
                            _mcuStream = stream;
                            continue;
                        }
                        else if (ackBuffer[offset + 1] == 0xFC)
                        {
                            _imuStream = stream;
                            continue;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // タイムアウト → IMU
                    if (_imuStream == null)
                        _imuStream = stream;
                    continue;
                }
            }
            catch
            {
                // エラー → IMU
                if (_imuStream == null)
                    _imuStream = stream;
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
                    break;
                }
            }
        }
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

        _logger.LogInformation("IMUデータストリームを開始しました");
        int frameCount = 0;

        // IMU有効化（ストリーム開始時）
        try
        {
            await SendImuEnableCommandAsync(enable: true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IMUの有効化に失敗しました");
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
            catch
            {
                // 無効化失敗は致命的ではないため、例外を吐かない
            }

            _logger.LogInformation("IMUデータストリームを終了しました（{FrameCount}フレーム）", frameCount);
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
                if (VitureLumaPacket.TryParseImuPacket(buffer.AsSpan(0, bytesRead), out var imuData, skipCrcValidation: true) && imuData != null)
                    return imuData;
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルは正常な終了
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "IMUストリームの読み取り中にエラーが発生しました");
        }

        return null;
    }

    /// <summary>
    /// IMU有効化/無効化コマンドを送信
    /// </summary>
    private async Task SendImuEnableCommandAsync(bool enable, CancellationToken cancellationToken = default)
    {
        if (_mcuStream == null)
            return;

        var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable, _messageCounter++);

        // デバイスの MaxOutputReportLength に基づいてバッファを作成
        var writeBuffer = new byte[_mcuStream.MaxOutputReportLength];
        writeBuffer[0] = 0x00; // Report ID
        Array.Copy(cmdPacket, 0, writeBuffer, 1, Math.Min(cmdPacket.Length, writeBuffer.Length - 1));

        try
        {
            // MCUストリーム「のみ」に送信
            await _mcuStream.WriteAsync(writeBuffer, cancellationToken);

            // ACK受信待機（タイムアウト付き）
            var ackBuffer = new byte[_mcuStream.MaxInputReportLength];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(500));

            try
            {
                await _mcuStream.ReadAsync(ackBuffer, 0, ackBuffer.Length, cts.Token);
            }
            catch (OperationCanceledException)
            {
                // ACKタイムアウトは許容
            }

            await Task.Delay(100, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "IMUコマンドの送信に失敗しました");
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_isConnected && _mcuStream != null)
        {
            try
            {
                // IMU無効化コマンドを送信
                await SendImuEnableCommandAsync(enable: false);
            }
            catch
            {
                // ディスポーズ中のエラーは無視
            }
        }

        await _hidProvider.DisposeAsync();

        _isConnected = false;
        _disposed = true;
        
        _logger.LogInformation("デバイスを切断しました");
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}


