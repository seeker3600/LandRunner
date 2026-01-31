namespace GlassBridge;

/// <summary>
/// GlassBridge ���C�u�����̎g�p��
/// 
/// ���O�o�̓N���X�ꗗ�F
/// - ImuDeviceManager.cs (lines 7, 27, 48, 64, 82, 97)
/// - VitureLumaDevice.cs (lines 13, 103, 145, 175, 235, 285, 322, 365, 384)
/// - HidStreamProvider.cs (lines 13, 35, 47, 56, 63)
/// 
/// �ڍ׃��O���x���F
/// - DEBUG: �ڑ��t���[�A�t���[�����J�E���g�A�f�o�C�X���
/// - INFO: �d�v�ȃC�x���g�i�ڑ������A�X�g���[���J�n/�I���j
/// - WARN: �񕜉\�ȃG���[�i�f�o�C�X���o���s�Ȃǁj
/// - ERROR: ���쎸�s�i�ڑ����s�A�R�}���h���M���s�j
/// - TRACE: �ł��ڍׁi�ʐM���e�A�p�P�b�g���j- �{�Ԋ��ł͖���������
/// </summary>
public static class UsageExample
{
    /// <summary>
    /// VITURE Luma �f�o�C�X���� IMU �f�[�^�X�g���[�~���O���擾
    /// 
    /// ���O�o�̓N���X�F
    /// - ImuDeviceManager.ConnectAsync() [line 27-30]
    /// - VitureLumaDevice.ConnectAsync() [line 13]
    /// - VitureLumaDevice.InitializeAsync() [line 103-119]
    /// - VitureLumaDevice.IdentifyStreamsAsync() [line 145-223]
    /// - VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
    /// - VitureLumaDevice.TryReadImuDataAsync() [line 322-348]
    /// - VitureLumaDevice.SendImuEnableCommandAsync() [line 365-414]
    /// </summary>
    public static async Task StreamImuDataAsync()
    {
        using var manager = new ImuDeviceManager();

        // �f�o�C�X�ɐڑ�
        // ���O�o��: ImuDeviceManager.ConnectAsync() [line 27-30]
        var device = await manager.ConnectAsync();
        if (device == null)
        {
            Console.WriteLine("Failed to connect to VITURE Luma device");
            return;
        }

        await using (device)
        {
            var cts = new CancellationTokenSource(TimeSpan.FromSeconds(10));

            // IMU �f�[�^�X�g���[�~���O�擾
            // ���O�o��: VitureLumaDevice.GetImuDataStreamAsync() [line 235-298]
            //         VitureLumaDevice.SendImuEnableCommandAsync() [line 365-414]
            //         VitureLumaDevice.TryReadImuDataAsync() [line 322-348]
            await foreach (var imuData in device.GetImuDataStreamAsync(cts.Token))
            {
                var euler = imuData.EulerAngles;
                var quat = imuData.Quaternion;

                Console.WriteLine(
                    $"Timestamp: {imuData.Timestamp}, " +
                    $"Euler(R/P/Y): {euler.Roll:F2}/{euler.Pitch:F2}/{euler.Yaw:F2}, " +
                    $"Quat(W/X/Y/Z): {quat.W:F3}/{quat.X:F3}/{quat.Y:F3}/{quat.Z:F3}");
            }
        }
    }

    /// <summary>
    /// �e�X�g�p�F���b�N�f�o�C�X�̎g�p��
    /// 
    /// ���O�o�́F�Ȃ��i���b�N�f�o�C�X�̓��M���O���Ή��j
    /// </summary>
    public static async Task MockDeviceExampleAsync()
    {
        // �e�X�g�p�̃��b�N�f�o�C�X���쐬
        var mockDevice = MockImuDevice.CreateWithPeriodicData(
            counter =>
            {
                // �J�E���^�[�l�Ɋ�Â��ĉ�]�l�𐶐�
                float angle = counter * 5.0f; // 5�x����]
                return new ImuData
                {
                    Quaternion = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f),
                    EulerAngles = new EulerAngles(angle, angle * 0.5f, angle * 1.5f),
                    Timestamp = (uint)counter,
                    MessageCounter = counter
                };
            },
            intervalMs: 16,
            maxIterations: 10
        );

        await using (mockDevice)
        {
            await foreach (var data in mockDevice.GetImuDataStreamAsync())
            {
                Console.WriteLine($"Mock data - Euler: {data.EulerAngles}");
            }
        }
    }
}
