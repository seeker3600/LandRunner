namespace GlassBridge.Internal.HID;

using System.Runtime.CompilerServices;

/// <summary>
/// �e�X�g�p�̃��b�NHID�X�g���[���i�񓯊��Ή��j
/// </summary>
internal sealed class MockHidStream : IHidStream
{
    /// <summary>
    /// �f�t�H���g�̃��|�[�g���iVITURE �f�o�C�X�ɍ��킹���l�j
    /// Report ID (1 byte) + Report Data (64 bytes) = 65 bytes
    /// </summary>
    public const int DefaultReportLength = 65;

    private readonly IAsyncEnumerable<ImuData> _dataStream;
    private readonly CancellationToken _cancellationToken;
    private IAsyncEnumerator<ImuData>? _enumerator;
    private ImuData? _currentData;
    private bool _disposed;
    private int _readOffset;

    public bool IsOpen => !_disposed;

    /// <summary>
    /// �ő���̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    public int MaxInputReportLength { get; }

    /// <summary>
    /// �ő�o�̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    public int MaxOutputReportLength { get; }

    public MockHidStream(
        IAsyncEnumerable<ImuData> dataStream,
        CancellationToken cancellationToken = default,
        int maxInputReportLength = DefaultReportLength,
        int maxOutputReportLength = DefaultReportLength)
    {
        _dataStream = dataStream ?? throw new ArgumentNullException(nameof(dataStream));
        _cancellationToken = cancellationToken;
        MaxInputReportLength = maxInputReportLength;
        MaxOutputReportLength = maxOutputReportLength;
    }

    public async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockHidStream));

        // ����Ăяo�����Ƀf�[�^���擾
        if (_enumerator == null)
        {
            _enumerator = _dataStream.GetAsyncEnumerator(_cancellationToken);
            if (!await FetchNextDataAsync())
                return 0;
        }

        // ���݂̃f�[�^���Ȃ��ꍇ�͎��̃f�[�^���擾
        if (_currentData == null)
        {
            if (!await FetchNextDataAsync())
                return 0; // �f�[�^�X�g���[�����I��
        }

        // ���݂̃f�[�^���V���A���C�Y���ăo�b�t�@�ɋl�߂�
        if (_currentData != null)
        {
            var packet = SerializeImuData(_currentData);
            int bytesToCopy = Math.Min(packet.Length - _readOffset, count);
            Array.Copy(packet, _readOffset, buffer, offset, bytesToCopy);

            _readOffset += bytesToCopy;

            // ���̃f�[�^������
            if (_readOffset >= packet.Length)
            {
                _readOffset = 0;
                _currentData = null; // ���� ReadAsync �Ŏ��f�[�^���擾
            }

            return bytesToCopy;
        }

        return 0;
    }

    public async Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(MockHidStream));

        // ���b�N�ł͉������Ȃ�
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
            // �G���[���͏I��
        }

        return false;
    }

    /// <summary>
    /// ImuData���o�C�g�z��ɃV���A���C�Y�iVITURE �p�P�b�g�`���j
    /// VitureLumaPacket.TryParseImuPacket �ƌ݊����̂���`��
    /// </summary>
    private static byte[] SerializeImuData(ImuData data)
    {
        var buffer = new byte[64];

        // �w�b�_�iVITURE IMU �f�[�^�p�P�b�g�j
        buffer[0] = 0xFF;
        buffer[1] = 0xFC;  // IMU Data

        // CRC �͊ȗ����i0�ł��j
        buffer[2] = 0x00;
        buffer[3] = 0x00;

        // Payload length�ioffset 4-5�A���g���G���f�B�A���j
        ushort payloadLen = 30; // �ȗ���
        buffer[4] = (byte)(payloadLen & 0xFF);
        buffer[5] = (byte)((payloadLen >> 8) & 0xFF);

        // Timestamp�ioffset 6-9�A���g���G���f�B�A���j
        buffer[6] = (byte)(data.Timestamp & 0xFF);
        buffer[7] = (byte)((data.Timestamp >> 8) & 0xFF);
        buffer[8] = (byte)((data.Timestamp >> 16) & 0xFF);
        buffer[9] = (byte)((data.Timestamp >> 24) & 0xFF);

        // Reserved�ioffset 10-13�j
        buffer[10] = 0x00;
        buffer[11] = 0x00;
        buffer[12] = 0x00;
        buffer[13] = 0x00;

        // Command ID�ioffset 14-15�j
        buffer[14] = 0x00;
        buffer[15] = 0x00;

        // Message counter�ioffset 16-17�A���g���G���f�B�A���j
        buffer[16] = (byte)(data.MessageCounter & 0xFF);
        buffer[17] = (byte)((data.MessageCounter >> 8) & 0xFF);

        // IMU �f�[�^�ioffset 18-29�j
        // raw0, raw1, raw2 (3 x float32 = 12 bytes�A�r�b�O�G���f�B�A��)
        var euler = data.EulerAngles;
        
        // yaw = -raw0
        float raw0 = -euler.Yaw;
        // roll = -raw1
        float raw1 = -euler.Roll;
        // pitch = raw2
        float raw2 = euler.Pitch;

        // �r�b�O�G���f�B�A�� float32
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


