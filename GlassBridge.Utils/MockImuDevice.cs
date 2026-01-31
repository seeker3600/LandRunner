namespace GlassBridge.Utils;

using System.Runtime.CompilerServices;
using GlassBridge;

/// <summary>
/// テスト用のモックIMUデバイス実装
/// </summary>
public sealed class MockImuDevice : IImuDevice
{
    private readonly Func<CancellationToken, IAsyncEnumerable<ImuData>>? _dataSourceFactory;
    private bool _disposed;

    public bool IsConnected => !_disposed;

    /// <summary>
    /// モックデバイスを作成
    /// </summary>
    /// <param name="dataSourceFactory">IMUデータを生成するファクトリ関数（オプション）</param>
    public MockImuDevice(Func<CancellationToken, IAsyncEnumerable<ImuData>>? dataSourceFactory = null)
    {
        _dataSourceFactory = dataSourceFactory;
    }

    /// <summary>
    /// テスト用：単一のIMUデータを返すモックデバイスを作成
    /// </summary>
    public static MockImuDevice CreateWithStaticData(ImuData data)
    {
        return new MockImuDevice(_ => YieldStaticData(data));
    }

    /// <summary>
    /// テスト用：定期的にIMUデータを生成するモックデバイスを作成
    /// </summary>
    public static MockImuDevice CreateWithPeriodicData(
        Func<ushort, ImuData> dataFactory,
        int intervalMs = 16,
        int maxIterations = 100)
    {
        return new MockImuDevice(ct => GeneratePeriodicData(dataFactory, intervalMs, maxIterations, ct));
    }

    public async IAsyncEnumerable<ImuData> GetImuDataStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockImuDevice));

        if (_dataSourceFactory != null)
        {
            await foreach (var data in _dataSourceFactory(cancellationToken))
            {
                yield return data;
            }
        }
    }

    public async ValueTask DisposeAsync()
    {
        _disposed = true;
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }

    private static async IAsyncEnumerable<ImuData> YieldStaticData(ImuData data)
    {
        yield return data;
        await Task.CompletedTask;
    }

    private static async IAsyncEnumerable<ImuData> GeneratePeriodicData(
        Func<ushort, ImuData> dataFactory,
        int intervalMs,
        int maxIterations,
        [EnumeratorCancellation] CancellationToken cancellationToken)
    {
        for (ushort i = 0; i < maxIterations; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                break;

            yield return dataFactory(i);
            await Task.Delay(intervalMs, cancellationToken);
        }
    }
}
