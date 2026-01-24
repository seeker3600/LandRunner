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
    /// ImuDataをバイト配列にシリアライズ（簡易的な実装）
    /// 実際にはVitureLumaPacket.TryParseImuPacketと対応する形式が必要
    /// </summary>
    private static byte[] SerializeImuData(ImuData data)
    {
        // 簡易的なシリアライズ実装
        // 実際の形式はVitureLumaPacketのパースロジックに合わせる必要があります
        var buffer = new byte[64];

        // Quaternion (4 floats = 16 bytes)
        BitConverter.GetBytes(data.Quaternion.W).CopyTo(buffer, 0);
        BitConverter.GetBytes(data.Quaternion.X).CopyTo(buffer, 4);
        BitConverter.GetBytes(data.Quaternion.Y).CopyTo(buffer, 8);
        BitConverter.GetBytes(data.Quaternion.Z).CopyTo(buffer, 12);

        // EulerAngles (3 floats = 12 bytes)
        BitConverter.GetBytes(data.EulerAngles.Roll).CopyTo(buffer, 16);
        BitConverter.GetBytes(data.EulerAngles.Pitch).CopyTo(buffer, 20);
        BitConverter.GetBytes(data.EulerAngles.Yaw).CopyTo(buffer, 24);

        // Timestamp (4 bytes)
        BitConverter.GetBytes(data.Timestamp).CopyTo(buffer, 28);

        // MessageCounter (2 bytes)
        BitConverter.GetBytes(data.MessageCounter).CopyTo(buffer, 32);

        return buffer;
    }
}

