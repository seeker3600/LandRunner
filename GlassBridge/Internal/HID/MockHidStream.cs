namespace GlassBridge.Internal.HID;

using System.Runtime.CompilerServices;

/// <summary>
/// テスト用のモックHIDストリーム（非同期対応）
/// </summary>
internal sealed class MockHidStream : IHidStream
{
    private readonly IAsyncEnumerable<ImuData> _dataStream;
    private readonly CancellationToken _cancellationToken;
    private IAsyncEnumerator<ImuData>? _enumerator;
    private ImuData? _currentData;
    private bool _disposed;
    private int _readOffset;

    public bool IsOpen => !_disposed;

    public MockHidStream(IAsyncEnumerable<ImuData> dataStream, CancellationToken cancellationToken = default)
    {
        _dataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
        _cancellationToken = cancellationToken;
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockHidStream));

        // 初回呼び出し時にデータを取得
        if (_enumerator == null)
        {
            _enumerator = _dataStream.GetAsyncEnumerator(_cancellationToken);
            if (!await FetchNextDataAsync())
                return 0;
        }

        // 現在のデータがない場合は次のデータを取得
        if (_currentData == null)
        {
            if (!await FetchNextDataAsync())
                return 0; // データストリームが終了
        }

        // 現在のデータをシリアライズしてバッファに詰める
        if (_currentData != null)
        {
            var packet = SerializeImuData(_currentData);
            int bytesToCopy = Math.Min(packet.Length - _readOffset, count);
            Array.Copy(packet, _readOffset, buffer, offset, bytesToCopy);

            _readOffset += bytesToCopy;

            // 次のデータを準備
            if (_readOffset >= packet.Length)
            {
                _readOffset = 0;
                _currentData = null; // 次の ReadAsync で次データを取得
            }

            return bytesToCopy;
        }

        return 0;
    }

    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockHidStream));

        // モックでは何もしない
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_enumerator != null)
        {
            _enumerator.DisposeAsync().GetAwaiter().GetResult();
        }

        _disposed = true;
    }

    private async Task<bool> FetchNextDataAsync()
    {
        try
        {
            if (await _enumerator!.MoveNextAsync())
            {
                _currentData = _enumerator.Current;
                return true;
            }
        }
        catch
        {
            // エラー時は終了
        }

        return false;
    }

    /// <summary>
    /// ImuDataをバイト配列にシリアライズ（VITURE パケット形式）
    /// VitureLumaPacket.TryParseImuPacket と互換性のある形式
    /// </summary>
    private static byte[] SerializeImuData(ImuData data)
    {
        var buffer = new byte[64];

        // ヘッダ（VITURE IMU データパケット）
        buffer[0] = 0xFF;
        buffer[1] = 0xFC;  // IMU Data

        // CRC は簡略化（0でも可）
        buffer[2] = 0x00;
        buffer[3] = 0x00;

        // Payload length（offset 4-5、リトルエンディアン）
        ushort payloadLen = 30; // 簡略化
        buffer[4] = (byte)(payloadLen & 0xFF);
        buffer[5] = (byte)((payloadLen >> 8) & 0xFF);

        // Timestamp（offset 6-9、リトルエンディアン）
        buffer[6] = (byte)(data.Timestamp & 0xFF);
        buffer[7] = (byte)((data.Timestamp >> 8) & 0xFF);
        buffer[8] = (byte)((data.Timestamp >> 16) & 0xFF);
        buffer[9] = (byte)((data.Timestamp >> 24) & 0xFF);

        // Reserved（offset 10-13）
        buffer[10] = 0x00;
        buffer[11] = 0x00;
        buffer[12] = 0x00;
        buffer[13] = 0x00;

        // Command ID（offset 14-15）
        buffer[14] = 0x00;
        buffer[15] = 0x00;

        // Message counter（offset 16-17、リトルエンディアン）
        buffer[16] = (byte)(data.MessageCounter & 0xFF);
        buffer[17] = (byte)((data.MessageCounter >> 8) & 0xFF);

        // IMU データ（offset 18-29）
        // raw0, raw1, raw2 (3 x float32 = 12 bytes、ビッグエンディアン)
        var euler = data.EulerAngles;
        
        // yaw = -raw0
        float raw0 = -euler.Yaw;
        // roll = -raw1
        float raw1 = -euler.Roll;
        // pitch = raw2
        float raw2 = euler.Pitch;

        // ビッグエンディアン float32
        var bytes0 = BitConverter.GetBytes(raw0);
        if (BitConverter.IsLittleEndian)
            System.Array.Reverse(bytes0);
        bytes0.CopyTo(buffer, 18);

        var bytes1 = BitConverter.GetBytes(raw1);
        if (BitConverter.IsLittleEndian)
            System.Array.Reverse(bytes1);
        bytes1.CopyTo(buffer, 22);

        var bytes2 = BitConverter.GetBytes(raw2);
        if (BitConverter.IsLittleEndian)
            System.Array.Reverse(bytes2);
        bytes2.CopyTo(buffer, 26);

        // End marker
        buffer[30] = 0x03;

        return buffer;
    }
}


