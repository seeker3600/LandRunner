namespace GlassBridge.Internal;

using GlassBridge.Internal.HID;
using System.Runtime.CompilerServices;
using Microsoft.Extensions.Logging;

/// <summary>
/// VITURE�n�O���X�pIMU�f�o�C�X����
/// IMU/MCU�X�g���[���̔��ʂ͂��̃N���X���Ӗ��iVITURE�ŗL�̃h���C���m���j
/// </summary>
internal sealed class VitureLumaDevice : IImuDevice
{
    private static readonly ILogger<VitureLumaDevice> _logger = LoggerFactoryProvider.Instance.CreateLogger<VitureLumaDevice>();

    internal const int VendorId = 0x35CA;

    internal static readonly int[] SupportedProductIds =
    [
        0x1011, 0x1013, 0x1017,  // VITURE One
        0x1015, 0x101b,           // VITURE One Lite
        0x1019, 0x101d,           // VITURE Pro
        0x1121, 0x1141,           // VITURE Luma Pro
        0x1131                    // VITURE Luma
    ];

    private readonly IHidStreamProvider _hidProvider;

    // VITURE�ŗL�FIMU/MCU�X�g���[���i�h���C���m���j
    private IHidStream? _imuStream;
    private IHidStream? _mcuStream;

    private bool _isConnected;
    private bool _disposed;
    private ushort _messageCounter;

    public bool IsConnected => _isConnected && !_disposed;

    private VitureLumaDevice(IHidStreamProvider hidProvider)
    {
        _hidProvider = hidProvider ?? throw new ArgumentNullException(nameof(hidProvider));
        _messageCounter = 0;
    }

    /// <summary>
    /// �f�o�C�X�ɐڑ����AIMU�L�����R�}���h�𑗐M
    /// </summary>
    public static async Task<VitureLumaDevice?> ConnectAsync(CancellationToken cancellationToken = default)
    {
        // HidSharp�̔ėp���b�p�[���g�p
        var provider = new HidStreamProvider();
        return await ConnectWithProviderAsync(provider, cancellationToken);
    }

    /// <summary>
    /// �w�肳�ꂽ�v���o�C�_�Ńf�o�C�X���������i�e�X�g�p�j
    /// </summary>
    internal static async Task<VitureLumaDevice?> ConnectWithProviderAsync(
        IHidStreamProvider hidProvider,
        CancellationToken cancellationToken = default)
    {
        var device = new VitureLumaDevice(hidProvider);

        if (await device.InitializeAsync(cancellationToken))
            return device;

        await device.DisposeAsync();
        return null;
    }

    /// <summary>
    /// �f�o�C�X��������
    /// IMU/MCU�X�g���[���̔��ʂ��s���iVITURE�ŗL���W�b�N�j
    /// </summary>
    private async Task<bool> InitializeAsync(CancellationToken cancellationToken)
    {
        try
        {
            _logger.LogDebug("Device initialization started");

            // HidStreamProvider����S�X�g���[�����擾
            var allStreams = await _hidProvider.GetStreamsAsync(
                VendorId,
                SupportedProductIds,
                cancellationToken);
            if (allStreams.Count < 2)
            {
                _logger.LogError("Expected at least 2 streams, but found {StreamCount}", allStreams.Count);
                return false;
            }

            _logger.LogDebug("Found {StreamCount} streams, identifying IMU and MCU...", allStreams.Count);

            // VITURE�ŗL�FIMU/MCU�𔻕�
            await IdentifyStreamsAsync(allStreams, cancellationToken);

            if (_imuStream == null || _mcuStream == null)
            {
                _logger.LogError("Stream identification failed: IMU={ImuStreamOk}, MCU={McuStreamOk}", _imuStream != null, _mcuStream != null);
                return false;
            }

            _logger.LogInformation("Stream identification successful: IMU and MCU identified");

            // �X�g���[�����ʌ�AIMU�𖳌�������
            // GetImuDataStreamAsync �Ăяo�����ɂ����L�������邱�ƂŁA
            // �Â��f�[�^��USB�o�b�t�@�ɒ~�ς���邱�Ƃ�h��
            try
            {
                await SendImuEnableCommandAsync(enable: false, cancellationToken);
            }
            catch
            {
                // �����������̈ꕔ�Ȃ̂ŁA���s���Ă��V�X�e���͓���p��
            }

            _isConnected = true;
            _logger.LogDebug("Device initialization completed successfully");
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Initialize failed: {ErrorMessage}", ex.Message);
            return false;
        }
    }

    /// <summary>
    /// VITURE�ŗL�F�X�g���[������IMU/MCU�𔻕�
    /// �L���ȃR�}���h�p�P�b�g�𑗐M���ĉ������e�X�g
    /// �h�L�������g�Q�ƁF�u���M�ۂŃR�}���h�̈���������㔻�ʂ��Ă���v
    /// </summary>
    private async Task IdentifyStreamsAsync(IReadOnlyList<IHidStream> streams, CancellationToken cancellationToken)
    {
        // �V���v���Ȕ��ʁF�ŏ��̃X�g���[���� MCU�A2�Ԗڂ� IMU �Ƃ���
        // �i������ WebHID �̔��ʕ����Ɋ�Â��j
        _logger.LogDebug("Identifying IMU and MCU streams from {StreamCount} available streams", streams.Count);

        for (int i = 0; i < streams.Count; i++)
        {
            var stream = streams[i];
            _logger.LogDebug("Testing stream #{StreamIndex} for identification", i);

            try
            {
                // �L���� IMU enable �R�}���h�p�P�b�g�𑗐M
                var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable: true, messageCounter: 0);
                
                // �f�o�C�X�� MaxOutputReportLength �Ɋ�Â��ăo�b�t�@���쐬
                var writeBuffer = new byte[stream.MaxOutputReportLength];
                writeBuffer[0] = 0x00; // Report ID
                Array.Copy(cmdPacket, 0, writeBuffer, 1, Math.Min(cmdPacket.Length, writeBuffer.Length - 1));

                _logger.LogTrace("Sending IMU enable command to stream #{StreamIndex}, packet size: {PacketSize}", i, cmdPacket.Length);
                await stream.WriteAsync(writeBuffer, cancellationToken);

                // �����ҋ@�i�^�C���A�E�g�t���j
                // �f�o�C�X�� MaxInputReportLength �Ɋ�Â��ăo�b�t�@���쐬
                var ackBuffer = new byte[stream.MaxInputReportLength];
                using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
                cts.CancelAfter(TimeSpan.FromMilliseconds(100));

                try
                {
                    int bytesRead = await stream.ReadAsync(ackBuffer, 0, ackBuffer.Length, cts.Token);

                    // Report ID �I�t�Z�b�g�����o
                    int offset = (bytesRead > 1 && ackBuffer[0] == 0x00 && ackBuffer[1] == 0xFF) ? 1 : 0;

                    // ����������΂��̃X�g���[���� MCU
                    if (bytesRead > offset && ackBuffer[offset] == 0xFF)
                    {
                        // MCU ACK �� IMU �f�[�^���m�F
                        if (ackBuffer[offset + 1] == 0xFD)
                        {
                            _mcuStream = stream;
                            _logger.LogInformation("Stream #{StreamIndex} identified as MCU (ACK received: 0xFF 0xFD)", i);
                            continue;
                        }
                        else if (ackBuffer[offset + 1] == 0xFC)
                        {
                            // IMU �f�[�^���Ԃ��Ă���
                            _imuStream = stream;
                            _logger.LogInformation("Stream #{StreamIndex} identified as IMU (data received: 0xFF 0xFC)", i);
                            continue;
                        }
                    }
                }
                catch (OperationCanceledException)
                {
                    // �^�C���A�E�g �� IMU
                    if (_imuStream == null)
                    {
                        _imuStream = stream;
                        _logger.LogDebug("Stream #{StreamIndex} identified as IMU (timeout on ACK wait)", i);
                    }
                    continue;
                }
            }
            catch (Exception ex)
            {
                // �G���[ �� IMU
                if (_imuStream == null)
                {
                    _imuStream = stream;
                    _logger.LogDebug(ex, "Stream #{StreamIndex} identified as IMU (exception on write): {ErrorMessage}", i, ex.Message);
                }
            }
        }

        // �����蓖�ẴX�g���[�����c��Ɋ��蓖�Ă�
        if (_mcuStream == null && _imuStream != null)
        {
            for (int i = 0; i < streams.Count; i++)
            {
                if (streams[i] != _imuStream)
                {
                    _mcuStream = streams[i];
                    _logger.LogDebug("MCU stream assigned to stream #{StreamIndex} (fallback)", i);
                    break;
                }
            }
        }
        else if (_imuStream == null && _mcuStream != null)
        {
            for (int i = 0; i < streams.Count; i++)
            {
                if (streams[i] != _mcuStream)
                {
                    _imuStream = streams[i];
                    _logger.LogDebug("IMU stream assigned to stream #{StreamIndex} (fallback)", i);
                    break;
                }
            }
        }

        _logger.LogInformation("Stream identification complete: IMU={ImuStreamOk}, MCU={McuStreamOk}", _imuStream != null, _mcuStream != null);
    }

    /// <summary>
    /// IMU�f�[�^�X�g���[�����擾
    /// ���̃��\�b�h�Ăяo������IMU��L�������A�I�����ɖ���������
    /// ����ɂ��A�Â��f�[�^��USB�o�b�t�@�ɒ~�ς����̂�h���A
    /// �Ăяo�����_�ł̍ŐV�f�[�^���擾�ł���
    /// </summary>
    public async IAsyncEnumerable<ImuData> GetImuDataStreamAsync([EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        if (!IsConnected || _imuStream == null)
            throw new InvalidOperationException("Device is not connected");

        _logger.LogInformation("IMU data stream started");
        int frameCount = 0;

        // IMU�L�����i�X�g���[���J�n���j
        try
        {
            await SendImuEnableCommandAsync(enable: true, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to enable IMU: {ErrorMessage}", ex.Message);
            throw;
        }

        try
        {
            // �f�o�C�X�� MaxInputReportLength �Ɋ�Â��ăo�b�t�@���쐬
            // VITURE�d�l: Report ID (1 byte) + Report Size (64 bytes) = 65 bytes
            var buffer = new byte[_imuStream.MaxInputReportLength];

            while (!cancellationToken.IsCancellationRequested && IsConnected)
            {
                var imuData = await TryReadImuDataAsync(_imuStream, buffer, cancellationToken);

                if (imuData != null)
                {
                    frameCount++;
                    if (frameCount % 1000 == 0)
                    {
                        _logger.LogDebug("Streamed {FrameCount} IMU data frames", frameCount);
                    }
                    yield return imuData;
                }
                else
                {
                    await Task.Delay(1, cancellationToken);
                }
            }
        }
        finally
        {
            // IMU�������i�X�g���[���I���� - ��O�����K�����s�j
            try
            {
                await SendImuEnableCommandAsync(enable: false, cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to disable IMU: {ErrorMessage}", ex.Message);
                // ���������s�͒v���I�ł͂Ȃ����߁A��O��f���Ȃ�
            }

            _logger.LogInformation("IMU data stream ended after {FrameCount} frames", frameCount);
        }
    }

    /// <summary>
    /// HID�X�g���[������IMU�f�[�^��ǂݍ������Ƃ���i�񓯊��j
    /// </summary>
    private async Task<ImuData?> TryReadImuDataAsync(IHidStream stream, byte[] buffer, CancellationToken cancellationToken)
    {
        try
        {
            int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length, cancellationToken);

            if (bytesRead > 0)
            {
                _logger.LogTrace("Read {BytesCount} bytes from IMU stream", bytesRead);

                if (VitureLumaPacket.TryParseImuPacket(buffer.AsSpan(0, bytesRead), out var imuData, skipCrcValidation: true) && imuData != null)
                {
                    _logger.LogTrace("Successfully parsed IMU packet: Counter={MessageCounter}, Timestamp={Timestamp}", 
                        imuData.MessageCounter, imuData.Timestamp);
                    return imuData;
                }
                else
                {
                    _logger.LogDebug("Failed to parse IMU packet from {BytesCount} bytes", bytesRead);
                }
            }
        }
        catch (OperationCanceledException)
        {
            // �L�����Z���͐���ȏI��
            _logger.LogDebug("IMU read cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Error reading from IMU stream: {ErrorMessage}", ex.Message);
        }

        return null;
    }

    /// <summary>
    /// IMU�L����/�������R�}���h�𑗐M
    /// </summary>
    private async Task SendImuEnableCommandAsync(bool enable, CancellationToken cancellationToken = default)
    {
        if (_mcuStream == null)
        {
            _logger.LogWarning("MCU stream is null, cannot send IMU {EnableState} command", enable ? "enable" : "disable");
            return;
        }

        var cmdPacket = VitureLumaPacket.BuildImuEnableCommand(enable, _messageCounter++);

        // �f�o�C�X�� MaxOutputReportLength �Ɋ�Â��ăo�b�t�@���쐬
        var writeBuffer = new byte[_mcuStream.MaxOutputReportLength];
        writeBuffer[0] = 0x00; // Report ID
        Array.Copy(cmdPacket, 0, writeBuffer, 1, Math.Min(cmdPacket.Length, writeBuffer.Length - 1));

        _logger.LogDebug("Sending IMU {EnableState} command, MessageCounter={MessageCounter}, PacketSize={PacketSize}", 
            enable ? "enable" : "disable", _messageCounter - 1, cmdPacket.Length);

        try
        {
            // MCU�X�g���[���u�̂݁v�ɑ��M
            await _mcuStream.WriteAsync(writeBuffer, cancellationToken);
            _logger.LogTrace("IMU {EnableState} command sent to MCU", enable ? "enable" : "disable");

            // ACK��M�ҋ@�i�^�C���A�E�g�t���j
            // �f�o�C�X�� MaxInputReportLength �Ɋ�Â��ăo�b�t�@���쐬
            var ackBuffer = new byte[_mcuStream.MaxInputReportLength];
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            cts.CancelAfter(TimeSpan.FromMilliseconds(500));

            try
            {
                int bytesRead = await _mcuStream.ReadAsync(ackBuffer, 0, ackBuffer.Length, cts.Token);

                // Report ID �I�t�Z�b�g�����o
                int offset = (bytesRead > 1 && ackBuffer[0] == 0x00 && ackBuffer[1] == 0xFF) ? 1 : 0;

                if (bytesRead >= offset + 2)
                {
                    _logger.LogTrace("MCU response: {ResponseByte0:X2} {ResponseByte1:X2}", ackBuffer[offset], ackBuffer[offset + 1]);
                    
                    if (ackBuffer[offset] == 0xFF && ackBuffer[offset + 1] == 0xFD)
                    {
                        _logger.LogDebug("Received MCU ACK for IMU {EnableState} command", enable ? "enable" : "disable");
                    }
                }
                else
                {
                    _logger.LogDebug("MCU response received but invalid length: {BytesCount}", bytesRead);
                }
            }
            catch (OperationCanceledException)
            {
                _logger.LogDebug("MCU ACK timeout (acceptable in some cases)");
            }

            await Task.Delay(100, cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Failed to send IMU command: {ErrorMessage}", ex.Message);
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed)
            return;

        _logger.LogDebug("Disposing VitureLumaDevice");

        if (_isConnected && _mcuStream != null)
        {
            try
            {
                // IMU�������R�}���h�𑗐M
                await SendImuEnableCommandAsync(enable: false);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Error disabling IMU during dispose: {ErrorMessage}", ex.Message);
            }
        }

        await _hidProvider.DisposeAsync();

        _isConnected = false;
        _disposed = true;
        
        _logger.LogInformation("VitureLumaDevice disposed");
    }

    public void Dispose()
    {
        DisposeAsync().GetAwaiter().GetResult();
    }
}


