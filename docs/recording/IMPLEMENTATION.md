## IMU �f�[�^�L�^�E�Đ��@�\

GlassBridge �� IMU �f�o�C�X����̕����f�[�^���L�^�E�Đ��ł���@�\���g�ݍ��܂�Ă��܂��B

### �T�v

- **�L�^**: �f�o�C�X����󂯎���� IMU �f�[�^�� JSON Lines �`���ŕۑ�
- **�Đ�**: �L�^���ꂽ JSON �t�@�C������ Mock �f�o�C�X�̂悤�ɍĐ��\
- **�t�H�[�}�b�g**: �l�Ԃ��ǂݎ��� JSON Lines �`���i`.jsonl`�j
- **�e�X�g�Ή�**: �e�X�g�p�p�t�H�[�}���X���͂ɍœK

### �t�@�C���\��

�L�^���ʂ̃t�@�C���͈ȉ��̂悤�ȍ\���ł��F

```
output_directory/
������ frames_0.jsonl          # IMU �t���[���f�[�^ (JSON Lines�`��)
������ metadata_0.json         # �L�^�Z�b�V�����̃��^�f�[�^
������ frames_1.jsonl
������ metadata_1.json
```

#### frames_*.jsonl �̌`��

```json
{"timestamp":0,"messageCounter":0,"quaternion":{"w":1.0,"x":0.0,"y":0.0,"z":0.0},"eulerAngles":{"roll":0.0,"pitch":0.0,"yaw":0.0},"rawBytes":"AAAAAA=="}
{"timestamp":10,"messageCounter":1,"quaternion":{"w":1.0,"x":0.01,"y":0.02,"z":0.03},"eulerAngles":{"roll":1.0,"pitch":2.0,"yaw":3.0},"rawBytes":"AAAAAA=="}
```

#### metadata_*.json �̌`��

```json
{
  "recordedAt": "2026-01-25T12:34:56.1234567Z",
  "frameCount": 1000,
  "sampleRate": 100,
  "format": "jsonl"
}
```

### ��v�ȃN���X

#### �L�^�֘A

- **RecordingHidStream**: `IHidStream` �����b�v���ċL�^�@�\��ǉ�
- **RecordingHidStreamProvider**: HID �X�g���[���v���o�C�_�[���L�^�@�\�Ń��b�v
- **ImuFrameRecord**: ImuData �� JSON �`���ŕ\��
- **ImuRecordingSession**: �L�^�Z�b�V�����̃��^�f�[�^

#### �Đ��֘A

- **RecordedHidStream**: JSON Lines �t�@�C������ `IHidStream` �Ƃ��čĐ�
- **ReplayHidStreamProvider**: �L�^�f�B���N�g������Đ��X�g���[�����쐬

### �g�p���@

#### �f�o�C�X����f�[�^���L�^

```csharp
var baseProvider = new HidStreamProvider(0x35CA, new[] { 0x1131 });
var recordingProvider = new RecordingHidStreamProvider(baseProvider, @"C:\IMU_Records");

var device = await VitureLumaDevice.ConnectWithProviderAsync(recordingProvider);

// IMU �f�[�^���擾�i�����ɋL�^�����j
await foreach (var imuData in device.GetImuDataStreamAsync())
{
    // �f�[�^����
}

await recordingProvider.FinalizeRecordingAsync();
await device.DisposeAsync();
```

#### �L�^���ꂽ�f�[�^���Đ�

```csharp
var replayProvider = new ReplayHidStreamProvider(@"C:\IMU_Records");
var device = await VitureLumaDevice.ConnectWithProviderAsync(replayProvider);

// �L�^���ꂽ�f�[�^�������ʂ�擾
await foreach (var imuData in device.GetImuDataStreamAsync())
{
    // �f�[�^�������E�e�X�g���s
}
```

#### �L�^�t�@�C����ǂݍ���

```csharp
// ���^�f�[�^��ǂݍ���
var metadataJson = File.ReadAllText("output/metadata_0.json");
var metadata = ImuRecordingSession.FromJson(metadataJson);

Console.WriteLine($"Frames: {metadata.FrameCount}");
Console.WriteLine($"Recorded: {metadata.RecordedAt}");

// �t���[����1�s���Ƃɓǂ�
using var reader = new StreamReader("output/frames_0.jsonl");
string? line;
while ((line = reader.ReadLine()) != null)
{
    var frameRecord = ImuFrameRecord.FromJsonLine(line);
    Console.WriteLine($"Timestamp: {frameRecord.Timestamp}");
}
```

### �A�[�L�e�N�`��

```
�A�v���P�[�V�����w
    ��
IImuDevice (�ύX�Ȃ�)
    ��
VitureLumaDevice
    ��
[�L�^�E�Đ����b�p�[�w]
IHidStreamProvider
    ������ RecordingHidStreamProvider (�L�^�w)
    ������ ReplayHidStreamProvider (�Đ��w)
    ��
IHidStream
    ������ RecordingHidStream (�L�^�@�\�ǉ�)
    ������ RecordedHidStream (�Đ��@�\)
    ������ RealHidStream (���f�o�C�X)
    ������ MockHidStream (�e�X�g)
```

### ���񎖍�

1. **IImuDevice �C���^�[�t�F�[�X�͕ύX�Ȃ�** - �A�v���P�[�V�������ւ̉e���Ȃ�
2. **�����I�ȋL�^** - `VitureLumaDevice.GetImuDataStreamAsync()` ���Ăяo���Ă���ԂɋL�^
3. **�l�Ԃ��ǂ߂�f�[�^�`��** - JSON Lines �`���Ȃ̂� �e�L�X�g�G�f�B�^�Ŋm�F�\
4. **�P���ȍĐ�** - Mock �f�o�C�X�̂悤�ȋ@�\�Ȃ̂� �e�X�g�E���\���͂ɍœK
5. **�X�P�[���u��** - �����Z�b�V�����̓����L�^���\

### �e�X�g�Ή�

- ? JSON �V���A���C�U�[����
- ? ���^�f�[�^�̕ۑ��E�ǂݍ���
- ? JSON Lines �t�H�[�}�b�g�̌���
- ? �t�@�C�� I/O
- ? �X�g���[���Đ�

���ׂẴe�X�g����������Ă��܂��B
