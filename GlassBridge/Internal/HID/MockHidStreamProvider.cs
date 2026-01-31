namespace GlassBridge.Internal.HID;

/// <summary>
/// �e�X�g�p�̃��b�N HID �X�g���[���v���o�C�_�[
/// IHidStreamProvider �̎����C���^�[�t�F�[�X�ɍ��킹��
/// IMU/MCU ��2�̃X�g���[����Ԃ�
/// </summary>
internal sealed class MockHidStreamProvider : IHidStreamProvider
{
    private readonly Func<CancellationToken, IAsyncEnumerable<ImuData>> _imuDataStreamFactory;
    private bool _disposed;

    public MockHidStreamProvider(Func<CancellationToken, IAsyncEnumerable<ImuData>> imuDataStreamFactory)
    {
        _imuDataStreamFactory = imuDataStreamFactory ?? throw new ArgumentNullException(nameof(imuDataStreamFactory));
    }

    public async Task<IReadOnlyList<IHidStream>> GetStreamsAsync(
        int vendorId,
        int[] productIds,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockHidStreamProvider));

        var imuDataStream = _imuDataStreamFactory(cancellationToken);
        
        // �e�X�g�p: MCU/IMU �̏����ŕԂ� (MCU ���ŏ�)
        // MCU �X�g���[��: �R�}���h�ɉ����� ACK �p�P�b�g��Ԃ�
        IHidStream mcuStream = new MockMcuStream();
        
        // IMU �X�g���[��: �e�X�g�f�[�^��Ԃ�
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
/// MCU �X�g���[���p�̃��b�N����
/// �R�}���h�ɉ����� ACK �p�P�b�g��Ԃ�
/// </summary>
internal sealed class MockMcuStream : IHidStream
{
    /// <summary>
    /// �f�t�H���g�̃��|�[�g���iVITURE �f�o�C�X�ɍ��킹���l�j
    /// Report ID (1 byte) + Report Data (64 bytes) = 65 bytes
    /// </summary>
    public const int DefaultReportLength = 65;

    private bool _disposed;
    private int _readCount;

    public bool IsOpen => !_disposed;

    /// <summary>
    /// �ő���̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    public int MaxInputReportLength { get; } = DefaultReportLength;

    /// <summary>
    /// �ő�o�̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    public int MaxOutputReportLength { get; } = DefaultReportLength;

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockMcuStream));

        // 1�񂾂� ACK ��Ԃ��ďI��
        if (_readCount >= 1)
        {
            return 0;
        }

        // ACK �p�P�b�g�𐶐� (�w�b�_: 0xFF 0xFD)
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

        // ���b�N: �������݂͉������Ȃ�
        await Task.CompletedTask;
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _disposed = true;
        await Task.CompletedTask;
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}

