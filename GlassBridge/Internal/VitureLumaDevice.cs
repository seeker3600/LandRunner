namespace GlassBridge.Internal;

using GlassBridge.Internal.HID;
using System.Runtime.CompilerServices;

/// <summary>
/// VITURE系グラス用IMUデバイス実装
/// IMU/MCUストリームの判別はこのクラスが責務（VITURE固有のドメイン知識）
/// </summary>
internal sealed class VitureLumaDevice : IImuDevice
{
    private const int VendorId = 0x35CA;

    // サポート対象の Product IDs
    // - VITURE One: 0x1011, 0x1013, 0x1017
    // - VITURE One Lite: 0x1015, 0x101b
    // - VITURE Pro: 0x1019, 0x101d
    // - VITURE Luma Pro: 0x1121, 0x1141
    // - VITURE Luma: 0x1131
    private static readonly int[] SupportedProductIds = new[]
    {
        0x1011, 0x1013, 0x1017,  // VITURE One
        0x1015, 0x101b,           // VITURE One Lite
        0x1019, 0x101d,           // VITURE Pro
        0x1121, 0x1141,           // VITURE Luma Pro
        0x1131                    // VITURE Luma
    };

    private const int ReadBufferSize = 64;

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
        var provider = new HidStreamProvider(VendorId, SupportedProductIds);
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
            var allStreams = await _hidProvider.GetStreamsAsync(cancellationToken);
            if (allStreams.Count < 2)
                return false;

            // VITURE固有：IMU/MCUを判別
            await IdentifyStreamsAsync(allStreams, cancellationToken);

            if (_imuStream == null || _mcuStream == null)
            {
                System.Diagnostics.Debug.WriteLine($"Stream identification failed: IMU={(_imuStream != null)}, MCU={(_mcuStream != null)}");
                return false;
            }

            // コマンド送信でデバイス初期化
            await SendImuEnableCommandAsync(enable: true, cancellationToken);

            _isConnected = true;
            return true;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Initialize failed: {ex.Message}");
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
                var writeBuffer = new byte[cmdPacket.Length + 1];
                writeBuffer[0] = 0x00; // Report ID
                Array.Copy(cmdPacket, 0, writeBuffer, 1, cmdPacket.Length);

                await stream.WriteAsync(writeBuffer, cancellationToken);

                // 応答待機（タイムアウト付き）
                var ackBuffer = new byte[ReadBufferSize];
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMilliseconds(100));

                try
                {
                    int bytesRead = await stream.ReadAsync(ackBuffer, 0, ackBuffer.Length, cts.Token);

                    // 応答があればこのストリームが MCU
                    if (bytesRead > 0 && ackBuffer[0] == 0xFF)
                    {
                        // MCU ACK か IMU データか確認
                        if (ackBuffer[1] == 0xFD)
                        {
                            _mcuStream = stream;
                            continue;
                        }
                        else if (ackBuffer[1] == 0xFC)
                        {
                            // IMU データが返ってきた
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
    /// </summary>
    public async IAsyncEnumerable<ImuData> GetImuDataStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _imuStream == null)
            throw new InvalidOperationException("Device is not connected");

        var buffer = new byte[ReadBufferSize];

        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            var imuData = await TryReadImuDataAsync(_imuStream, buffer, cancellationToken);

            if (imuData != null)
            {
                yield return imuData;
            }
            else
            {
                await Task.Delay(1, cancellationToken);
            }
        }
    }

    /// <summary>
    /// HIDストリームからIMUデータを読み込もうとする（非同期）
    /// </summary>
    private static async Task<ImuData?> TryReadImuDataAsync(IHidStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        try
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (bytesRead > 0 &&
                VitureLumaPacket.TryParseImuPacket(buffer.AsSpan(0, bytesRead), out var imuData) &&
                imuData != null)
            {
                return imuData;
            }
        }
        catch
        {
            // 読み込みエラーは無視
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

        var writeBuffer = new byte[cmdPacket.Length + 1];
        writeBuffer[0] = 0x00; // Report ID
        Array.Copy(cmdPacket, 0, writeBuffer, 1, cmdPacket.Length);

        try
        {
            // MCUストリーム「のみ」に送信
            await _mcuStream.WriteAsync(writeBuffer, cancellationToken);

            // ACK受信待機（タイムアウト付き）
            var ackBuffer = new byte[ReadBufferSize];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(500));

            try
            {
                int bytesRead = await _mcuStream.ReadAsync(ackBuffer, 0, ackBuffer.Length, cts.Token);

                if (bytesRead >= 2 && ackBuffer[0] == 0xFF && ackBuffer[1] == 0xFD)
                {
                    System.Diagnostics.Debug.WriteLine("Received MCU ACK");
                }
            }
            catch (OperationCanceledException)
            {
                System.Diagnostics.Debug.WriteLine("MCU ACK timeout (acceptable in some cases)");
            }

            await Task.Delay(100, cancellationToken);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Failed to send IMU command: {ex.Message}");
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
                // エラーは無視
            }
        }

        await _hidProvider.DisposeAsync();

        _isConnected = false;
        _disposed = true;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}


