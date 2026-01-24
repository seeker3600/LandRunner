namespace GlassBridge.Internal.HID;

/// <summary>
/// テスト用のモックHIDストリームプロバイダ
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
        IHidStream stream = new MockHidStream(dataStream, cancellationToken);
        return new[] { stream };
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await Task.CompletedTask;
    }
}

