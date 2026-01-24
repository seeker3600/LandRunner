namespace GlassBridge.Internal.HID;

/// <summary>
/// テスト用のモックHIDストリームプロバイダ
/// IHidStreamProviderの汎用インターフェースに準拠
/// IMU/MCUの2つのストリームを返す
/// </summary>
internal sealed class MockHidStreamProvider : IHidStreamProvider
{
    private readonly Func<CancellationToken, IAsyncEnumerable<ImuData>> _imuDataStreamFactory;
    private bool _disposed;

    public MockHidStreamProvider(Func<CancellationToken, IAsyncEnumerable<ImuData>> imuDataStreamFactory)
    {
        _imuDataStreamFactory = imuDataStreamFactory ?? throw new ArgumentNullException(nameof(imuDataStreamFactory));
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockHidStreamProvider));

        var imuDataStream = _imuDataStreamFactory(cancellationToken);
        
        // テスト用：MCU/IMUの順序で返す（MCUが最初）
        // MCUストリーム：コマンドに応答するACKパケットを返す
        IHidStream mcuStream = new MockMcuStream();
        
        // IMUストリーム：テストデータを返す
        IHidStream imuStream = new MockHidStream(imuDataStream, cancellationToken);
        
        return new[] { mcuStream, imuStream };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await Task.CompletedTask;
    }
}

/// <summary>
/// MCU ストリーム用のモック実装
/// コマンドに応答する ACK パケットを返す
/// </summary>
internal sealed class MockMcuStream : IHidStream
{
    private bool _disposed;
    private int _readCount;

    public bool IsOpen => !_disposed;

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockMcuStream));

        // 1回だけ ACK を返して終了
        if (_readCount >= 1)
        {
            return 0;
        }

        // ACK パケットを生成（ヘッダ: 0xFF 0xFD）
        var ackPacket = new byte[64];
        ackPacket[0] = 0xFF;  // Header byte 0
        ackPacket[1] = 0xFD;  // Header byte 1 (MCU ACK)

        int bytesToCopy = Math.Min(ackPacket.Length, count);
        Array.Copy(ackPacket, 0, buffer, offset, bytesToCopy);

        _readCount++;

        await Task.Delay(1, cancellationToken);
        return bytesToCopy;
    }

    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockMcuStream));

        // コマンド受け取り
        await Task.Delay(1, cancellationToken);
    }

    public async ValueTask DisposeAsync()
    {
        Dispose();
        await ValueTask.CompletedTask;
    }

    public void Dispose()
    {
        _disposed = true;
    }
}






