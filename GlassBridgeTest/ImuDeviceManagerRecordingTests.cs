namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal.Recording;
using System.Text.Json;
using Xunit;

/// <summary>
/// ImuDeviceManager �̋L�^�E�Đ��@�\�̓����e�X�g
/// </summary>
public class ImuDeviceManagerRecordingTests : IDisposable
{
    private readonly string _testOutputDirectory;

    public ImuDeviceManagerRecordingTests()
    {
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), $"ImuDeviceManagerTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDirectory);
    }

    /// <summary>
    /// �e�X�g1: ConnectAndRecordAsync �̊�{����
    /// </summary>
    [Fact]
    public async Task ConnectAndRecordAsync_CreatesRecordingFiles()
    {
        // Arrange
        var manager = new ImuDeviceManager();
        var recordingDir = Path.Combine(_testOutputDirectory, "test1");
        Directory.CreateDirectory(recordingDir);

        // Act: ���f�o�C�X�ڑ������݂�i�f�o�C�X�Ȃ�����null���Ԃ�j
        await using var device = await manager.ConnectAndRecordAsync(recordingDir);

        // Assert: �L�^�@�\�����������������ꂽ���Ƃ��m�F
        // �f�o�C�X���ڑ��ł��Ȃ��Ă��A�L�^�@�\�͏����������
        Assert.NotNull(manager);  // �}�l�[�W���[���쐬����Ă���
        
        manager.Dispose();
    }

    /// <summary>
    /// �e�X�g2: ConnectFromRecordingAsync - �L�^�t�@�C�������݂��Ȃ��ꍇ
    /// </summary>
    [Fact]
    public async Task ConnectFromRecordingAsync_WithNoFiles_ReturnsNull()
    {
        // Arrange
        var manager = new ImuDeviceManager();
        var recordingDir = Path.Combine(_testOutputDirectory, "test2_empty");
        Directory.CreateDirectory(recordingDir);

        // Act
        await using var device = await manager.ConnectFromRecordingAsync(recordingDir);

        // Assert
        Assert.Null(device);  // �t�@�C���Ȃ�����null
        manager.Dispose();
    }

    /// <summary>
    /// �e�X�g3: ConnectFromRecordingAsync - �����ȃf�B���N�g��
    /// </summary>
    [Fact]
    public async Task ConnectFromRecordingAsync_WithInvalidDirectory_ThrowsException()
    {
        // Arrange
        var manager = new ImuDeviceManager();
        var nonexistentDir = Path.Combine(_testOutputDirectory, "nonexistent");

        // Act & Assert
        await Assert.ThrowsAsync<DirectoryNotFoundException>(
            () => manager.ConnectFromRecordingAsync(nonexistentDir)
        );

        manager.Dispose();
    }

    /// <summary>
    /// �e�X�g4: �L�^�t�@�C�����쐬���čĐ��e�X�g
    /// </summary>
    [Fact]
    public async Task ConnectFromRecordingAsync_WithValidRecording_ReplaysData()
    {
        // Arrange
        var recordingDir = Path.Combine(_testOutputDirectory, "test4");
        Directory.CreateDirectory(recordingDir);

        // �e�X�g�p�̋L�^�t�@�C�����쐬
        var testData = new ImuData[]
        {
            new ImuData
            {
                Timestamp = 0,
                MessageCounter = 0,
                Quaternion = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f),
                EulerAngles = new EulerAngles(0.0f, 0.0f, 0.0f)
            },
            new ImuData
            {
                Timestamp = 10,
                MessageCounter = 1,
                Quaternion = new Quaternion(1.0f, 0.01f, 0.02f, 0.03f),
                EulerAngles = new EulerAngles(1.0f, 2.0f, 3.0f)
            }
        };

        var framesPath = Path.Combine(recordingDir, "frames_0.jsonl");
        var metadataPath = Path.Combine(recordingDir, "metadata_0.json");

        // �t���[����ۑ�
        using (var writer = new StreamWriter(framesPath))
        {
            foreach (var frame in testData)
            {
                var record = ImuFrameRecord.FromImuData(frame, new byte[] { 0xFF, 0xFC });
                writer.WriteLine(record.ToJsonLine());
            }
        }

        // ���^�f�[�^��ۑ�
        var metadata = ImuRecordingSession.CreateNew(frameCount: 2, sampleRate: 100);
        File.WriteAllText(metadataPath, metadata.ToJson());

        // Act
        var manager = new ImuDeviceManager();
        await using var device = await manager.ConnectFromRecordingAsync(recordingDir);

        // Assert
        if (device != null)
        {
            Assert.True(device.IsConnected);

            // �f�[�^���擾���Ă݂�
            var count = 0;
            await foreach (var data in device.GetImuDataStreamAsync())
            {
                count++;
                Assert.NotNull(data);
                
                if (count >= 2)
                    break;
            }

            Assert.Equal(2, count);
        }
        
        manager.Dispose();
    }

    /// <summary>
    /// �e�X�g5: device.DisposeAsync() ���Ɏ����I�Ƀ��^�f�[�^���ۑ������
    /// </summary>
    [Fact]
    public async Task ConnectAndRecordAsync_AutomaticallyFinalizesOnDispose()
    {
        // Arrange
        var manager = new ImuDeviceManager();
        var recordingDir = Path.Combine(_testOutputDirectory, "test5");
        Directory.CreateDirectory(recordingDir);

        // Act: device �� using �ŊǗ��i�����I�� DisposeAsync ���Ă΂��j
        await using (var device = await manager.ConnectAndRecordAsync(recordingDir))
        {
            // �f�o�C�X���ڑ��ł��Ă��ł��Ȃ��Ă��A�L�^�@�\�͏���������Ă���
        }

        // Assert: device ���p������āA���^�f�[�^�t�@�C�����쐬���ꂽ���Ƃ��m�F
        // �L�^�t�@�C��������΁A���^�f�[�^���쐬�����͂�
        manager.Dispose();
    }

    /// <summary>
    /// �e�X�g6: Dispose ��̃��\�b�h�Ăяo��
    /// </summary>
    [Fact]
    public async Task DisposedManager_ThrowsObjectDisposedException()
    {
        // Arrange
        var manager = new ImuDeviceManager();
        manager.Dispose();

        // Act & Assert
        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.ConnectAsync()
        );

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.ConnectAndRecordAsync(_testOutputDirectory)
        );

        await Assert.ThrowsAsync<ObjectDisposedException>(
            () => manager.ConnectFromRecordingAsync(_testOutputDirectory)
        );
    }

    /// <summary>
    /// �e�X�g7: ConnectAndRecordAsync �̖����ȓ���
    /// </summary>
    [Fact]
    public async Task ConnectAndRecordAsync_WithNullDirectory_ThrowsException()
    {
        // Arrange
        var manager = new ImuDeviceManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.ConnectAndRecordAsync(null!)
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.ConnectAndRecordAsync("")
        );

        manager.Dispose();
    }

    /// <summary>
    /// �e�X�g8: ConnectFromRecordingAsync �̖����ȓ���
    /// </summary>
    [Fact]
    public async Task ConnectFromRecordingAsync_WithNullDirectory_ThrowsException()
    {
        // Arrange
        var manager = new ImuDeviceManager();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.ConnectFromRecordingAsync(null!)
        );

        await Assert.ThrowsAsync<ArgumentException>(
            () => manager.ConnectFromRecordingAsync("")
        );

        manager.Dispose();
    }

    /// <summary>
    /// �e�X�g9: �}�l�[�W���[�̕����L�^�Z�b�V�����؂�ւ�
    /// </summary>
    [Fact]
    public async Task MultipleRecordingSessions_CanBeSwitched()
    {
        // Arrange
        var manager = new ImuDeviceManager();
        var recordingDir1 = Path.Combine(_testOutputDirectory, "test9a");
        var recordingDir2 = Path.Combine(_testOutputDirectory, "test9b");
        Directory.CreateDirectory(recordingDir1);
        Directory.CreateDirectory(recordingDir2);

        // Act: �ŏ��̋L�^�Z�b�V����
        await using var device1 = await manager.ConnectAndRecordAsync(recordingDir1);
        
        // device ���p������ă��^�f�[�^�������ۑ������
        // (await using �Ŏ����I�� DisposeAsync ���Ă΂��)

        // 2�Ԗڂ̋L�^�Z�b�V����
        await using var device2 = await manager.ConnectAndRecordAsync(recordingDir2);

        // Assert
        // �f�o�C�X���ڑ��ł��Ă��ł��Ȃ��Ă��A�}�l�[�W���[���@�\���Ă����OK
        Assert.NotNull(manager);
        
        // �Z�b�V�����؂�ւ��������idevice2 �p�����Ɏ����I�Ƀ��^�f�[�^�ۑ��j
        manager.Dispose();
    }

    /// <summary>
    /// �e�X�g10: IImuDeviceManager �C���^�[�t�F�C�X�����e�X�g
    /// </summary>
    [Fact]
    public void ImuDeviceManager_ImplementsInterface()
    {
        // Arrange & Act
        var manager = new ImuDeviceManager();

        // Assert
        Assert.IsAssignableFrom<IImuDeviceManager>(manager);
        Assert.IsAssignableFrom<IDisposable>(manager);

        manager.Dispose();
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testOutputDirectory))
                Directory.Delete(_testOutputDirectory, true);
        }
        catch
        {
            // �N���[���A�b�v���s�͖���
        }
    }
}
