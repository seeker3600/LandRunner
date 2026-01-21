using HidSharp;
using System.Runtime.CompilerServices;

namespace GlassBridge;

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
    
    private const int ReadTimeoutMs = 1000;
    private const int ReadBufferSize = 64;

    private readonly List<HidDevice> _devices;
    private readonly List<HidStream?> _streams;
    private bool _isConnected;
    private bool _disposed;
    private ushort _messageCounter;

    public bool IsConnected => _isConnected && !_disposed;

    private VitureLumaDevice(List<HidDevice> devices, List<HidStream?> streams)
    {
        _devices = devices;
        _streams = streams;
        _isConnected = devices.Count > 0;
        _messageCounter = 0;
    }

    /// <summary>
    /// デバイスに接続し、IMU有効化コマンドを送信
    /// </summary>
    public static async Task<VitureLumaDevice?> ConnectAsync(CancellationToken cancellationToken = default)
    {
        var devices = new List<HidDevice>();
        var streams = new List<HidStream?>();

        // VID/PIDで列挙（サポート対象のすべてのPIDを試す）
        foreach (var productId in SupportedProductIds)
        {
            foreach (var device in DeviceList.Local.GetHidDevices(VendorId, productId))
            {
                try
                {
                    var stream = device.Open();
                    if (stream != null)
                    {
                        devices.Add(device);
                        streams.Add(stream);
                    }
                }
                catch
                {
                    // デバイスオープン失敗は無視
                }
            }
        }

        if (devices.Count == 0)
            return null;

        var vitureLuma = new VitureLumaDevice(devices, streams);

        try
        {
            // IMU有効化コマンドを送信
            await vitureLuma.SendImuEnableCommandAsync(enable: true, cancellationToken);
            return vitureLuma;
        }
        catch
        {
            // コマンド送信失敗時はリソースをクリーンアップ
            await vitureLuma.DisposeAsync();
            return null;
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

            // 全デバイスから読み込み
            foreach (var stream in _streams.Where(s => s != null))
            {
                if (stream == null)
                    continue;

                var imuData = TryReadImuData(stream, buffer);
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
    /// HIDストリームからIMUデータを読み込もうとする
    /// </summary>
    private static ImuData? TryReadImuData(HidStream stream, byte[] buffer)
    {
        try
        {
            int bytesRead = stream.Read(buffer, 0, buffer.Length);

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
    /// IMU有効化/無効化コマンドを送信
    /// </summary>
    private async Task SendImuEnableCommandAsync(bool enable, CancellationToken cancellationToken = default)
    {
        var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable, _messageCounter++);

        // 全デバイスにコマンドを送信
        foreach (var stream in _streams.Where(s => s != null))
        {
            if (stream == null)
                continue;

            try
            {
                // Report ID付きで送信（Report ID = 0x00）
                var writeBuffer = new byte[cmdPacket.Length + 1];
                writeBuffer[0] = 0x00; // Report ID
                Array.Copy(cmdPacket, 0, writeBuffer, 1, cmdPacket.Length);

                stream.Write(writeBuffer);

                // 応答を待つ
                await Task.Delay(100, cancellationToken);
            }
            catch
            {
                // 送信エラーは無視
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
                await SendImuEnableCommandAsync(enable: false);
            }
            catch
            {
                // エラーは無視
            }
        }

        // ストリームをクローズ
        foreach (var stream in _streams)
        {
            stream?.Dispose();
        }

        _isConnected = false;
        _disposed = true;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}
