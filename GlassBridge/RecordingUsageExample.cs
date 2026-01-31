namespace GlassBridge;

using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using GlassBridge.Internal.Recording;

/// <summary>
/// IMU �f�[�^�̋L�^�E�Đ��@�\�̎g�p��
/// 
/// ���O�o�̓N���X�ꗗ�F
/// - ImuDeviceManager.cs [line 48-76]
/// - VitureLumaDevice.cs [line 13, 103, 145, 175, 235, 285, 322, 365]
/// - HidStreamProvider.cs [line 13, 35, 47, 56, 63]
/// - RecordingHidStream.cs [line 13, 30, 55, 72, 81, 99]
/// 
/// �L�^�t���[�F
/// 1. HidStreamProvider.GetStreamsAsync() [HidStreamProvider line 35-56]
/// 2. RecordingHidStreamProvider �Ń��b�v
/// 3. VitureLumaDevice.ConnectWithProviderAsync() [VitureLumaDevice line 13-119]
/// 4. GetImuDataStreamAsync() �Ńf�[�^�擾�E�����L�^ [VitureLumaDevice line 235-298]
/// 5. RecordingHidStream.FinalizeAsync() �Ń��^�f�[�^�ۑ� [RecordingHidStream line 99-104]
/// </summary>
public class RecordingUsageExample
{
    /// <summary>
    /// �f�o�C�X���� IMU �f�[�^���L�^����
    /// 
    /// ���O�o�̓N���X�F
    /// - ImuDeviceManager.ConnectAndRecordAsync() [line 48-76]
    ///   - HidStreamProvider.GetStreamsAsync() [line 35-56]
    ///   - VitureLumaDevice.ConnectWithProviderAsync() [line 13]
    ///   - VitureLumaDevice.InitializeAsync() [line 103-119]
    ///   - VitureLumaDevice.IdentifyStreamsAsync() [line 145-223]
    /// - VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
    /// - RecordingHidStream.ReadAsync() [line 30-63]
    /// - RecordingHidStream.FinalizeAsync() [line 99-104]
    /// </summary>
    public static async Task RecordFromDeviceAsync(string outputDirectory)
    {
        using var manager = new ImuDeviceManager();

        // �f�o�C�X�ɐڑ����ċL�^�J�n
        // ���O�o��: ImuDeviceManager.ConnectAndRecordAsync() [line 48-76]
        //         HidStreamProvider.GetStreamsAsync() [line 35-56]
        //         VitureLumaDevice.InitializeAsync() [line 103-119]
        //         VitureLumaDevice.IdentifyStreamsAsync() [line 145-223]
        var device = await manager.ConnectAndRecordAsync(outputDirectory);
        if (device == null)
            throw new InvalidOperationException("Failed to connect to device for recording");

        try
        {
            // IMU �f�[�^�X�g���[�~���O�擾
            // ���O�o��: VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
            //         VitureLumaDevice.SendImuEnableCommandAsync() [line 365-414]
            //         RecordingHidStream.ReadAsync() [line 30-63]
            //         FrameCount �� 100 �̔{���Ń��O�o�� [line 55-58]
            var count = 0;
            await foreach (var imuData in device.GetImuDataStreamAsync())
            {
                Console.WriteLine($"Timestamp: {imuData.Timestamp}, Roll: {imuData.EulerAngles.Roll}");
                
                count++;
                if (count >= 100)  // 100�t���[���L�^
                    break;
            }

            Console.WriteLine($"Recorded {count} frames to {outputDirectory}");
        }
        finally
        {
            // �f�o�C�X�j�����ARecordingHidStream.FinalizeAsync() ���Ă΂��
            // ���O�o��: RecordingHidStream.FinalizeAsync() [line 99-104]
            await device.DisposeAsync();
        }
    }

    /// <summary>
    /// �L�^���ꂽ�f�[�^���� Mock �f�o�C�X���Đ�����
    /// 
    /// ���O�o�̓N���X�F
    /// - ImuDeviceManager.ConnectFromRecordingAsync() [line 82-97]
    ///   - ReplayHidStreamProvider �C���X�^���X��
    ///   - VitureLumaDevice.ConnectWithProviderAsync() [line 13]
    ///   - VitureLumaDevice.InitializeAsync() [line 103-119]
    /// - VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
    /// - ReplayHidStreamProvider.GetStreamsAsync() [�����Q��]
    /// </summary>
    public static async Task ReplayFromRecordingAsync(string recordingDirectory)
    {
        using var manager = new ImuDeviceManager();

        // �L�^�f�[�^����Đ��f�o�C�X���쐬
        // ���O�o��: ImuDeviceManager.ConnectFromRecordingAsync() [line 82-97]
        //         VitureLumaDevice.InitializeAsync() [line 103-119]
        var device = await manager.ConnectFromRecordingAsync(recordingDirectory);
        if (device == null)
            throw new InvalidOperationException("Failed to create replay device");

        try
        {
            // IMU �f�[�^�X�g���[�~���O�Đ�
            // ���O�o��: VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
            //         VitureLumaDevice.TryReadImuDataAsync() [line 322-348]
            //         FrameCount �� 1000 �̔{���Ń��O�o�� [line 245-248]
            var count = 0;
            await foreach (var imuData in device.GetImuDataStreamAsync())
            {
                Console.WriteLine($"Replayed - Timestamp: {imuData.Timestamp}, Pitch: {imuData.EulerAngles.Pitch}");
                
                count++;
                if (count >= 50)  // 50�t���[���Đ�
                    break;
            }

            Console.WriteLine($"Replayed {count} frames from {recordingDirectory}");
        }
        finally
        {
            // �f�o�C�X�j��
            // ���O�o��: VitureLumaDevice.DisposeAsync() [line 415-425]
            await device.DisposeAsync();
        }
    }
}
