# GlassBridge

XR�O���X�����IMU�i�������葕�u�j�f�[�^���擾���邽�߂� .NET ���C�u�����B

**���ݑΉ��F** VITURE Luma�AVITURE Pro�AVITURE One�AVITURE One Lite�AVITURE Luma Pro

## �T�v

GlassBridge�́AWindows���VITURE�n�V�[�X���[�O���X����3DoF�i���[���A�s�b�`�A���[�j�̓����p���f�[�^��񓯊��X�g���[���Ŏ擾�ł��郉�C�u�����ł��B

HID�v���g�R���̏ڍׂ��B�����A�V���v���Ŕ񓯊��I��API��񋟂��܂��B�܂��A�e�X�g���ɂ̓��b�N�����ŗe�ՂɃV�~�����[�V�����ł��܂��B

## ����

- ? **�������f���Ή�** - VITURE Luma�EPro�EOne�n����T�|�[�g
- ? **�񓯊��X�g���[��** - `IAsyncEnumerable<ImuData>`�Ŏ��R�ȃf�[�^�t���[
- ? **�e�X�g�\** - �C���^�[�t�F�[�X�����ƃ��b�N����
- ? **�����t�H�[�}�b�g�Ή�** - �I�C���[�p�ƃN�H�[�^�j�I���̗������
- ? **CRC����** - �p�P�b�g�̐������m�F
- ? **���\�[�X�Ǘ�** - `IAsyncDisposable`�ɂ�鎩���N���[���A�b�v

## �v���W�F�N�g�\��

```
GlassBridge/
������ ���J API
��   ������ ImuData.cs                 IMU�f�[�^�^�irecord�j
��   ������ Interfaces.cs              �C���^�[�t�F�[�X��`
��   ������ ImuDeviceManager.cs        �f�o�C�X�ڑ��}�l�[�W���[
��   ������ MockImuDevice.cs           �e�X�g�p���b�N����
������ �������� (GlassBridge.Internal namespace)
    ������ VitureLumaDevice.cs        HID�f�o�C�X����
    ������ VitureLumaPacket.cs        �v���g�R���p�P�b�g����
    ������ Crc16Ccitt.cs              CRC-16�v�Z���[�e�B���e�B
```

### ���O���

- **GlassBridge** - ���JAPI�i`ImuDeviceManager`�A`ImuData` ���j
- **GlassBridge.Internal** - ���������ڍׁiHID�f�o�C�X�A�p�P�b�g�������j

## �C���X�g�[��

���̃��C�u�����̓\�����[�V�����̈ꕔ�Ƃ��Ċ܂܂�܂��B�v���W�F�N�g�t�@�C���ŎQ�Ƃ��Ă��������B

### �ˑ��p�b�P�[�W

- **HidSharp** 2.6.4 - HID�f�o�C�X�ʐM

### �v��

- **.NET 10** �ȏ�
- **Windows** (USB HID�ʐM�̂���)

## �N�C�b�N�X�^�[�g

### ��{�I�Ȏg�p���@

```csharp
using GlassBridge;

// �}�l�[�W���[���쐬
using var manager = new ImuDeviceManager();

// VITURE Luma�ɐڑ�
var device = await manager.ConnectAsync();
if (device == null)
{
    Console.WriteLine("�f�o�C�X��������܂���");
    return;
}

// IMU�f�[�^�X�g���[��������
await using (device)
{
    var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
    
    await foreach (var imuData in device.GetImuDataStreamAsync(cts.Token))
    {
        var euler = imuData.EulerAngles;
        var quat = imuData.Quaternion;
        
        Console.WriteLine($"Roll: {euler.Roll:F1}��, Pitch: {euler.Pitch:F1}��, Yaw: {euler.Yaw:F1}��");
        Console.WriteLine($"Quaternion: W={quat.W:F3}, X={quat.X:F3}, Y={quat.Y:F3}, Z={quat.Z:F3}");
    }
}
```

## API ���t�@�����X

### ImuDeviceManager

���[�U�[�����̃��C���G���g���[�|�C���g�B

#### `ConnectAsync(CancellationToken = default)`

VITURE Luma�f�o�C�X�����o���Đڑ����܂��B

**�߂�l:** `Task<IImuDevice?>` - �ڑ����ꂽ�f�o�C�X�A�܂��͐ڑ����s����`null`

```csharp
var device = await manager.ConnectAsync();
```

### IImuDevice

�ڑ����ꂽIMU�f�o�C�X��\���܂��B

#### `GetImuDataStreamAsync(CancellationToken = default)`

IMU�f�[�^�̔񓯊��X�g���[�����擾���܂��B

**�߂�l:** `IAsyncEnumerable<ImuData>`

```csharp
await foreach (var data in device.GetImuDataStreamAsync(cancellationToken))
{
    // �f�[�^����
}
```

#### `IsConnected`

�f�o�C�X���ڑ�����Ă��邩�������v���p�e�B�B

```csharp
if (device.IsConnected)
{
    // �f�o�C�X���ڑ���
}
```

### ImuData

IMU�f�[�^��\�����R�[�h�^�B

```csharp
public record ImuData
{
    public required Quaternion Quaternion { get; init; }
    public required EulerAngles EulerAngles { get; init; }
    public required uint Timestamp { get; init; }
    public required ushort MessageCounter { get; init; }
}
```

### Quaternion

�N�H�[�^�j�I���\���B

```csharp
public record Quaternion(float W, float X, float Y, float Z)
```

**���\�b�h:**
- `Conjugate()` - �����N�H�[�^�j�I�����v�Z
- `operator *(Quaternion q1, Quaternion q2)` - 2�̃N�H�[�^�j�I������Z

```csharp
var conjugate = quat.Conjugate();
var combined = quat1 * quat2;  // ��]�̍���
```

### EulerAngles

�I�C���[�p�\���i�x�P�ʁj�B

```csharp
public record EulerAngles(float Roll, float Pitch, float Yaw);
```

## �e�X�g

### ���b�N�f�o�C�X�̎g�p

�e�X�g���ɂ�`MockImuDevice`�Ŏ��f�o�C�X�̑��肪�ł��܂��B

#### �ÓI�f�[�^��Ԃ����b�N

```csharp
var testData = new ImuData
{
    Quaternion = Quaternion.Identity,
    EulerAngles = new EulerAngles(0, 0, 0),
    Timestamp = 0,
    MessageCounter = 0
};

var mockDevice = MockImuDevice.CreateWithStaticData(testData);
await using (mockDevice)
{
    await foreach (var data in mockDevice.GetImuDataStreamAsync())
    {
        Assert.Equal(testData, data);
    }
}
```

#### ����I�Ƀf�[�^�𐶐����郂�b�N

```csharp
var mockDevice = MockImuDevice.CreateWithPeriodicData(
    counter =>
    {
        float angle = counter * 5.0f;  // 5�x����]
        return new ImuData
        {
            Quaternion = Quaternion.Identity,
            EulerAngles = new EulerAngles(angle, angle * 0.5f, angle * 1.5f),
            Timestamp = (uint)counter,
            MessageCounter = counter
        };
    },
    intervalMs: 16,      // 60FPS����
    maxIterations: 100
);

await using (mockDevice)
{
    var count = 0;
    await foreach (var data in mockDevice.GetImuDataStreamAsync())
    {
        count++;
    }
    Assert.Equal(100, count);
}
```

#### �e�X�g�ł̃C���^�[�t�F�[�X���p

`IImuDevice`�C���^�[�t�F�[�X���g�p����΁A�������e�X�g���ɐ؂�ւ����܂��F

```csharp
public class ImuDataProcessor
{
    private readonly IImuDevice _device;

    public ImuDataProcessor(IImuDevice device)
    {
        _device = device;  // �R���X�g���N�^�C���W�F�N�V����
    }

    public async Task ProcessDataAsync()
    {
        await foreach (var data in _device.GetImuDataStreamAsync())
        {
            // ����
        }
    }
}

// �e�X�g��
[Fact]
public async Task TestWithMockDevice()
{
    var mockDevice = MockImuDevice.CreateWithStaticData(
        new ImuData { /* ... */ }
    );
    
    var processor = new ImuDataProcessor(mockDevice);
    await processor.ProcessDataAsync();
    
    // ����
}
```

## �Z�p�d�l

### �Ή��f�o�C�X

| �f�o�C�X | VID | PID | �T�|�[�g |
|---------|-----|-----|---------|
| VITURE Luma | 0x35CA | 0x1131 | ? |

### VITURE Luma�v���g�R��

�ڍׂ� `docs/hid/VITURE_Luma.md` ���Q�Ƃ��Ă��������B

#### �p�P�b�g�\��

- **IMU �f�[�^**: �w�b�_ `0xFF 0xFC`
- **MCU ACK**: �w�b�_ `0xFF 0xFD`
- **MCU �R�}���h**: �w�b�_ `0xFF 0xFE`

#### �f�[�^�`��

- **�I�C���[�p**: �r�b�O�G���f�B�A�� IEEE754 float32
- **�N�H�[�^�j�I��**: �I�C���[�p����ϊ�
- **CRC**: CRC-16-CCITT (polynomial 0x1021, initial 0xFFFF)

### IMU�f�[�^�X�V���[�g

VITURE Luma�͕W���Ŗ�60?100Hz�Ńf�[�^�𑗐M���܂��B

## �g���u���V���[�e�B���O

### �f�o�C�X��������Ȃ�

1. VITURE Luma��������USB�ڑ�����Ă��邩�m�F
2. ���̃A�v���P�[�V�����iSpaceWalker�Ȃǁj���O���X���g�p���Ă��Ȃ����m�F
3. �f�o�C�X�h���C�o���������C���X�g�[������Ă��邩�m�F
4. `ImuDeviceManager.ConnectAsync()`��`null`���Ԃ��ꂽ�ꍇ�A�f�o�C�X�Ǘ���ʂ�VITURE�O���X���F������Ă��邩�m�F

### �f�[�^�X�g���[�����~�܂�

1. �L�����Z���[�V�����̏�Ԃ��m�F
2. �f�o�C�X�̐ڑ���Ԃ��m�F (`IImuDevice.IsConnected`)
3. USB�ڑ����s����łȂ����m�F

### CRC �G���[�͎����I�ɃX�L�b�v����܂�

�j�������p�P�b�g�͎����I�ɔj������A���̗L���ȃp�P�b�g��҂��܂��B

## ���}�b�s���O

**�d�v:** ���}�b�s���O�͎������̕W���l�ł����A���@�ł̌��؂𐄏����܂��B

���݂̎����iWebXR�d�l�Ɋ�Â��j:
- `Yaw = -raw0`
- `Roll = -raw1`
- `Pitch = raw2`

���ۂ̃A�v���P�[�V�����Ŋ��҂ƈقȂ�ꍇ�́A�ȉ����m�F���Ă��������F

1. "�E������" �� Yaw���������邩
2. "�������" �� Pitch���������邩
3. "�E�ɌX����" �� Roll���������邩

## ���\�[�X�Ǘ�

`IImuDevice`��`IAsyncDisposable`���������Ă���A�ڑ���K�؂ɃN���[�Y���܂��F

```csharp
// using���Ŏ����I��Dispose����܂�
await using (var device = await manager.ConnectAsync())
{
    // �g�p
}  // �����Ŏ����I��IMU�������R�}���h�����M����܂�
```

## �g����

���㑼�̃f�o�C�X�ɑΉ�������ꍇ�́A�ȉ����������Ă��������F

1. `IImuDevice`�����������V�����f�o�C�X�N���X
2. �f�o�C�X�ŗL�̃v���g�R���p�[�T�[
3. `IImuDeviceManager.ConnectAsync()`�ŐV�f�o�C�X�̌��o��ǉ�

## ���C�Z���X

[�v���W�F�N�g�̃��C�Z���X�ɏ����܂�]

## �֘A���\�[�X

- [VITURE Luma HID �v���g�R���d�l](../docs/hid/VITURE_Luma.md)
- [bfvogel/viture-webxr-extension](https://github.com/bfvogel/viture-webxr-extension) - ���o�[�X�G���W�j�A�����O�����̏o�T
