# ImuDeviceManager �̋L�^�E�Đ��@�\ - �g�p�K�C�h

`ImuDeviceManager` �ɋL�^�E�Đ��@�\���g�ݍ��܂�Ă��܂��B�N���C�A���g�J���҂͊ȒP�ɗ��p�ł��܂��B

## ��{�I�Ȏg�p���@

### 1. �f�o�C�X����̃f�[�^�L�^

```csharp
using var manager = new ImuDeviceManager();

// �f�o�C�X�ɐڑ����ċL�^���J�n
await using var device = await manager.ConnectAndRecordAsync(@"C:\IMU_Records");
if (device == null)
{
    Console.WriteLine("Failed to connect to device");
    return;
}

// IMU �f�[�^�X�g���[�����擾
// �I������ C:\IMU_Records �� frames_0.jsonl, metadata_0.json �Ƃ��ĕۑ������
var count = 0;
await foreach (var imuData in device.GetImuDataStreamAsync())
{
    Console.WriteLine($"Timestamp: {imuData.Timestamp}, Roll: {imuData.EulerAngles.Roll}");
    
    count++;
    if (count >= 1000)  // 1000�t���[���L�^������I��
        break;
}

Console.WriteLine($"Recorded {count} frames");
// device.DisposeAsync() ���ɍŏI�I�Ƀ��^�f�[�^���ۑ������
```

### 2. �L�^���ꂽ�f�[�^�̍Đ��i�e�X�g�E�p�t�H�[�}���X���́j

```csharp
using var manager = new ImuDeviceManager();

// �L�^�t�@�C������Đ��f�o�C�X���쐬
await using var replayDevice = await manager.ConnectFromRecordingAsync(@"C:\IMU_Records");
if (replayDevice == null)
{
    Console.WriteLine("No recording files found");
    return;
}

// �L�^���ꂽ�f�[�^���X�g���[���ő��M
// ���f�o�C�X�Ɠ����C���^�[�t�F�[�X�ŗ��p�\
var count = 0;
await foreach (var imuData in replayDevice.GetImuDataStreamAsync())
{
    // �e�X�g�␫�\���͗p���W�b�N
    Console.WriteLine($"Replayed - Timestamp: {imuData.Timestamp}");
    
    count++;
    if (count >= 100)  // 100�t���[���Đ�������I��
        break;
}

Console.WriteLine($"Replayed {count} frames");
```

### 3. �ʏ�̃f�o�C�X�ڑ��i�ύX�Ȃ��j

```csharp
using var manager = new ImuDeviceManager();

// �ʏ�̃f�o�C�X�ڑ�
var device = await manager.ConnectAsync();
if (device == null)
{
    Console.WriteLine("Failed to connect to device");
    return;
}

try
{
    await foreach (var imuData in device.GetImuDataStreamAsync())
    {
        Console.WriteLine($"Timestamp: {imuData.Timestamp}");
    }
}
finally
{
    await device.DisposeAsync();
}
```

## API ���t�@�����X

### IImuDeviceManager

#### ConnectAsync()
���f�o�C�X�ɐڑ����܂��B�ύX�Ȃ��B

```csharp
Task<IImuDevice?> ConnectAsync(CancellationToken cancellationToken = default);
```

**�߂�l**: �ڑ����ꂽ�f�o�C�X�A�ڑ����s���� `null`

---

#### ConnectAndRecordAsync()
�f�o�C�X�ɐڑ����A�擾�����f�[�^���t�@�C���ɋL�^���܂��B

```csharp
Task<IImuDevice?> ConnectAndRecordAsync(
    string outputDirectory,
    CancellationToken cancellationToken = default);
```

**�p�����[�^**:
- `outputDirectory`: �L�^�t�@�C���̏o�̓f�B���N�g���i�Ȃ���΍쐬�j
- `cancellationToken`: �L�����Z���g�[�N���i�I�v�V�����j

**�߂�l**: �L�^�@�\�t���̃f�o�C�X�A�ڑ����s���� `null`

**�o�̓t�@�C��**:
- `frames_0.jsonl`: IMU �t���[���f�[�^�iJSON Lines�`���j
- `metadata_0.json`: �L�^�Z�b�V�����̃��^�f�[�^

**���^�f�[�^�̒���ۑ�**:
- `device.DisposeAsync()` ���ɍŏI�I�� `metadata_*.json` ���ۑ�����܂�
- `await using` ���g�p���Ċm���Ƀ��������������悤�ɂ��Ă�������

**��O**:
- `ArgumentException`: `outputDirectory` �� null �܂��� empty �̏ꍇ

---

#### ConnectFromRecordingAsync()
�L�^���ꂽ�t�@�C������ Mock �f�o�C�X���쐬���čĐ����܂��B

```csharp
Task<IImuDevice?> ConnectFromRecordingAsync(
    string recordingDirectory,
    CancellationToken cancellationToken = default);
```

**�p�����[�^**:
- `recordingDirectory`: �L�^�t�@�C�����ۑ�����Ă���f�B���N�g��
- `cancellationToken`: �L�����Z���g�[�N���i�I�v�V�����j

**�߂�l**: �Đ��p�� Mock �f�o�C�X�A�t�@�C���Ȃ���� `null`

**��O**:
- `ArgumentException`: `recordingDirectory` �� null �܂��� empty �̏ꍇ
- `DirectoryNotFoundException`: �f�B���N�g����������Ȃ��ꍇ

---

## ������F�e�X�g�V�i���I

### �e�X�g�̗���Ǝ��s

```csharp
public class ImuDataProcessingTests
{
    [Fact]
    public async Task ProcessingLogic_WithRecordedData()
    {
        using var manager = new ImuDeviceManager();
        
        // �L�^�t�@�C������Đ�
        await using var replayDevice = await manager.ConnectFromRecordingAsync(@"C:\TestRecordings");
        if (replayDevice == null)
            throw new InvalidOperationException("No recording found");

        var processingResults = new List<ProcessingResult>();
        
        // �f�[�^����
        await foreach (var imuData in replayDevice.GetImuDataStreamAsync())
        {
            var result = ProcessImuData(imuData);
            processingResults.Add(result);
            
            if (processingResults.Count >= 1000)
                break;
        }

        // ���ʊm�F
        Assert.NotEmpty(processingResults);
        Assert.All(processingResults, r => Assert.True(r.IsValid));
    }

    private ProcessingResult ProcessImuData(ImuData data)
    {
        // �J�X�^�����W�b�N
        return new ProcessingResult { IsValid = true };
    }
}
```

### �p�t�H�[�}���X�v��

```csharp
using var manager = new ImuDeviceManager();
var recordingDir = @"C:\BenchmarkRecordings";

await using var replayDevice = await manager.ConnectFromRecordingAsync(recordingDir);
if (replayDevice == null)
    throw new InvalidOperationException("No recording found");

var stopwatch = System.Diagnostics.Stopwatch.StartNew();
var frameCount = 0;

await foreach (var imuData in replayDevice.GetImuDataStreamAsync())
{
    // �x���`�}�[�N�Ώۂ̃��W�b�N
    var euler = imuData.EulerAngles;
    var quat = imuData.Quaternion;
    
    frameCount++;
    if (frameCount >= 10000)
        break;
}

stopwatch.Stop();
Console.WriteLine($"Processed {frameCount} frames in {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine($"Average: {stopwatch.ElapsedMilliseconds / (double)frameCount}ms per frame");
```

## �L�^�t�@�C���̍\��

### frames_*.jsonl
JSON Lines �`���̃t���[���f�[�^�B1�s��1�t���[���ł��B

```json
{"timestamp":0,"messageCounter":0,"quaternion":{"w":1.0,"x":0.0,"y":0.0,"z":0.0},"eulerAngles":{"roll":0.0,"pitch":0.0,"yaw":0.0},"rawBytes":"..."}
```

**�t�B�[���h**:
- `timestamp`: �t���[���̃^�C���X�^���v�iuint32�j
- `messageCounter`: ���b�Z�[�W�J�E���^�[�iushort�j
- `quaternion`: �N�H�[�^�j�I���iw, x, y, z�j
- `eulerAngles`: �I�C���[�p�iroll, pitch, yaw�j
- `rawBytes`: ���o�C�g��iBase64�G���R�[�h�j

### metadata_*.json
�L�^�Z�b�V�����̃��^�f�[�^�B**device.DisposeAsync() ���ɍŏI�I�ɍ쐬**����܂��B

```json
{
  "recordedAt": "2026-01-25T12:34:56.1234567Z",
  "frameCount": 1000,
  "sampleRate": 100,
  "format": "jsonl"
}
```

**�t�B�[���h**:
- `recordedAt`: �L�^�J�n�����iISO 8601�`���j
- `frameCount`: �t���[����
- `sampleRate`: �T���v�����O���[�g�iHz�j
- `format`: �t�@�C���`���i�ʏ� "jsonl"�j

## �G���[�n���h�����O

```csharp
using var manager = new ImuDeviceManager();

try
{
    // �L�^
    await using var device = await manager.ConnectAndRecordAsync(recordingDir);
    if (device == null)
    {
        Console.WriteLine("Device connection failed");
        return;
    }

    // �f�[�^�擾
    // device.DisposeAsync() ���ɍŏI�I�Ƀ��^�f�[�^���ۑ������
}
catch (ArgumentException ex)
{
    Console.WriteLine($"Invalid argument: {ex.Message}");
}
catch (DirectoryNotFoundException ex)
{
    Console.WriteLine($"Directory not found: {ex.Message}");
}
catch (ObjectDisposedException ex)
{
    Console.WriteLine($"Manager already disposed: {ex.Message}");
}
finally
{
    manager.Dispose();
}
```

## ���ӎ����E�x�X�g�v���N�e�B�X

1. **�t�@�C���V�X�e�� I/O**: �L�^���̓t�@�C���V�X�e���ւ̏������݂��������邽�߁A�f�B�X�N���x�ɍ��E����܂�
2. **�V�[�P���V����**: �Đ���1�t���[���P�ʂ�1�s�ǂݍ��ނ��߁A�����_���A�N�Z�X�ł��܂���
3. **�}���`�Z�b�V����**: �����}�l�[�W���[�œ����f�B���N�g���ɋL�^����ꍇ�A�قȂ�Z�b�V�������Ƃ� device �͉�����K�v�ł�
4. **���A���^�C���f�B���C**: �Đ��͋L�^���̃^�C���X�^���v���g�p���܂����A���s���x�̂���ɍ��E����܂�
5. **���^�f�[�^�ۑ�**: `device.DisposeAsync()` �ōŏI�I�Ƀ��^�f�[�^���ۑ�����邽�߁A`await using` �̎g�p���������܂�

## �e�X�g�d�l�ꗗ

? �f�o�C�X�ڑ����̃f�[�^�L�^
? �L�^�t�@�C������̍Đ�
? device.DisposeAsync() ���̃��^�f�[�^�ۑ�
? �}���`�Z�b�V�����؂�ւ�
? �G���[�n���h�����O�i�g���d�l�Ȃǁj
? �C���^�[�t�F�[�X�m�F

���ׂẴe�X�g����������Ă��܂��B
