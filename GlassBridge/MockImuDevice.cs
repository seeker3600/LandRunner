using System.Runtime.CompilerServices;

namespace GlassBridge;

/// <summary>
/// �e�X�g�p�̃��b�NIMU�f�o�C�X����
/// </summary>
public sealed class MockImuDevice : IImuDevice
{
    private readonly Func<CancellationToken, IAsyncEnumerable<ImuData>>? _dataSourceFactory;
    private bool _disposed;

    public bool IsConnected => !_disposed;

    /// <summary>
    /// ���b�N�f�o�C�X���쐬
    /// </summary>
    /// <param name="dataSourceFactory">IMU�f�[�^�𐶐�����t�@�N�g���֐��i�I�v�V�����j</param>
    public MockImuDevice(Func<CancellationToken, IAsyncEnumerable<ImuData>>? dataSourceFactory = null)
    {
        _dataSourceFactory = dataSourceFactory;
    }

    /// <summary>
    /// �e�X�g�p�F�P���IMU�f�[�^��Ԃ����b�N�f�o�C�X���쐬
    /// </summary>
    public static MockImuDevice CreateWithStaticData(ImuData data)
    {
        return new MockImuDevice(_ => YieldStaticData(data));
    }

    /// <summary>
    /// �e�X�g�p�F����I��IMU�f�[�^�𐶐����郂�b�N�f�o�C�X���쐬
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
        System.Collections.Generic.IAsyncEnumerable<int> cancellationToken)
    {
        for (ushort i = 0; i < maxIterations; i++)
        {
            yield return dataFactory(i);
            await Task.Delay(intervalMs);
        }
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
