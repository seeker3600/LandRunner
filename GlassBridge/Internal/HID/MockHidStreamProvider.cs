namespace GlassBridge.Internal.HID;

/// <summary>
/// テスト用のモックHIDストリームプロバイダ
/// IHidStreamProviderの汎用インターフェースに準拠
/// </summary>
internal sealed class MockHidStreamProvider : IHidStreamProvider
{
    private readonly Func<CancellationToken, IAsyncEnumerable<ImuData>> _dataStreamFactory;
    private bool _disposed;

    public MockHidStreamProvider(Func<CancellationToken, IAsyncEnumerable<ImuData>> dataStreamFactory)
    {
        _dataStreamFactory = dataStreamFactory ?? throw new ArgumentNullException(nameof(dataStreamFactory));
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockHidStreamProvider));

        var dataStream = _dataStreamFactory(cancellationToken);
        
        // テスト用：IMU/MCUの2つのストリームを返す
        IHidStream imuStream = new MockHidStream(dataStream, cancellationToken);
        IHidStream mcuStream = new MockHidStream(CreateAckStream(), cancellationToken);
        
        return new[] { imuStream, mcuStream };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await Task.CompletedTask;
    }

    /// <summary>
    /// MCU ACKストリーム用のダミーデータを生成
    /// </summary>
    private static async IAsyncEnumerable<ImuData> CreateAckStream()
    {
        // ACKは実装していないため、空のストリームを返す
        await Task.CompletedTask;
        yield break;
    }
}


