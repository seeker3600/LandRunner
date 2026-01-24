namespace GlassBridge.Internal;

using GlassBridge.Internal.HID;
using System.Runtime.CompilerServices;

/// <summary>
/// VITURE系グラス用IMUデバイス実装
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
    private IReadOnlyList<IHidStream> _streams = [];
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
        // VITURE固有: VID/PIDでプロバイダを生成
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
    /// デバイスを初期化し、ストリームを取得してIMU有効化コマンドを送信
    /// </summary>
    private async Task<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _streams = await _hidProvider.GetStreamsAsync(cancellationToken);
            if (_streams.Count == 0)
                return false;

            // VITURE固有: 全ストリームにIMU有効化コマンドを送信
            await SendImuEnableCommandToAllStreamsAsync(enable: true, cancellationToken);

            _isConnected = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// VITURE固有: 全ストリームにIMUコマンドを送信
    /// </summary>
    private async Task SendImuEnableCommandToAllStreamsAsync(bool enable, CancellationToken cancellationToken)
    {
        var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable, _messageCounter++);

        // Report ID付きで送信（VITURE仕様）
        var writeBuffer = new byte[cmdPacket.Length + 1];
        writeBuffer[0] = 0x00; // Report ID
        Array.Copy(cmdPacket, 0, writeBuffer, 1, cmdPacket.Length);

        foreach (var stream in _streams)
        {
            try
            {
                await stream.WriteAsync(writeBuffer, cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to send IMU command: {ex.Message}");
            }
        }
    }

    /// <summary>
    /// IMUデータストリームを取得
    /// </summary>
    public async IAsyncEnumerable<ImuData> GetImuDataStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConnected)
            throw new InvalidOperationException("Device is not connected");

        var buffer = new byte[ReadBufferSize];

        while (!cancellationToken.IsCancellationRequested && IsConnected)
        {
            bool dataReceived = false;

            // 全デバイスから読み込み（複数パスの想定）
            foreach (var stream in _streams)
            {
                var imuData = await TryReadImuDataAsync(stream, buffer, cancellationToken);
                if (imuData != null)
                {
                    dataReceived = true;
                    yield return imuData;
                }
            }

            // データがない場合は少し待機
            if (!dataReceived)
            {
                try
                {
                    await Task.Delay(1, cancellationToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
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

            if (bytesRead == buffer.Length &&
                VitureLumaPacket.TryParseImuPacket(buffer, out var imuData) &&
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
    /// IMU無効化コマンドを送信
    /// </summary>
    private async Task SendImuDisableCommandAsync(CancellationToken cancellationToken = default)
    {
        var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable: false, _messageCounter++);

        // Report ID付きで送信（VITURE仕様）
        var writeBuffer = new byte[cmdPacket.Length + 1];
        writeBuffer[0] = 0x00; // Report ID
        Array.Copy(cmdPacket, 0, writeBuffer, 1, cmdPacket.Length);

        foreach (var stream in _streams)
        {
            try
            {
                await stream.WriteAsync(writeBuffer, cancellationToken);
                await Task.Delay(100, cancellationToken);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine(
                    $"Failed to send IMU disable command: {ex.Message}");
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        if (_isConnected)
        {
            try
            {
                // IMU無効化コマンドを送信
                await SendImuDisableCommandAsync();
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

