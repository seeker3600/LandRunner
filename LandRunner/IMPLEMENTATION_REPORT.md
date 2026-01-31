# LandRunner ����������

## ? �����������e

### 1. **IMU ���A���^�C���\���A�v���P�[�V����**
- VITURE Luma �����IMU�f�[�^��WPF�ŕ\��
- ���A���^�C���X�e�[�^�X�\���i�X�e�[�^�X�o�[�j
- 3D���̉����iX/Y/Z���� Yaw ��]���j
- Euler�p�x��Quaternion�l�̕\��

### 2. **���O�o�͋@�\**
- **�f�o�b�O���O**: `debug_<timestamp>.log` - �����t�����O
- **IMU�f�[�^ CSV**: `imu_data_<timestamp>.csv` - �Z���T�[�f�[�^�� CSV�`���ŋL�^
- �ۑ���: `%AppData%/LandRunner/`

### 3. **MVVM �p�^�[���i�x�X�g�v���N�e�B�X�j**
```
LandRunner/
������ Models/ImuLogger.cs              �� ���M���O�E�f�[�^�o��
������ ViewModels/
��   ������ ViewModelBase.cs             �� INotifyPropertyChanged����
��   ������ RelayCommand.cs              �� ICommand (�����E�񓯊��Ή�)
��   ������ MainWindowViewModel.cs       �� ��ԊǗ��E���W�b�N
������ Views/
    ������ MainWindow.xaml              �� UI���C�A�E�g�iDataBinding�j
    ������ MainWindow.xaml.cs           �� CodeBehind�i�ŏ����j
```

### 4. **�e�X�g�i�S10�� ? ���i�j**

#### ImuLoggerTests (5��)
- `ImuLogger_Initialize_CreatesLogFiles` ?
- `ImuLogger_LogDebug_WritesMessage` ?
- `ImuLogger_LogImuData_WritesCsvRow` ?
- `ImuLogger_MultipleDataPoints_PreservesOrder` ?
- `ImuLogger_Dispose_ClosesFiles` ?

#### DeviceConnectionIntegrationTests (4��)
- `ImuDeviceManager_CreateInstance_ShouldNotThrow` ?
- `MockDevice_StreamData_ProducesData` ?
- `ImuData_EulerAngles_ShouldBeAccurate` ?
- `Quaternion_Operations_ShouldWork` ?

#### LoggerThreadSafetyTests (1��)
- `ImuLogger_ConcurrentWrites_ShouldNotCorrupt` ?

**�e�X�g����: ���� 10/10 (���s 0)**

---

## ?? ��v�@�\

### �X�e�[�^�X�o�[�i�㕔�j
- �ڑ���ԕ\��
- ���A���^�C�����b�Z�[�W�J�E���g
- �^�C���X�^���v�\��

### �r�W���A���C�[�[�V�����i�����j
- X/Y/Z���̕`��i��/��/�j
- Yaw�p�x�ɂ���]���\���i���F�j
- ���_�}�[�N

### �f�[�^�\���p�l���i�E���j
- **Euler Angles**: Roll, Pitch, Yaw�i�x�j
- **Quaternion**: W, X, Y, Z
- **���^�f�[�^**: Timestamp, Message Counter

### �R���g���[���i�����j
- Connect Device �{�^��
- Disconnect �{�^��
- �X�e�[�^�X�e�L�X�g

---

## ??? MVVM �p�^�[���̎���

### ViewModelBase
```csharp
public class ViewModelBase : INotifyPropertyChanged
{
    // PropertyChanged �C�x���g�����Ǘ�
    // SetProperty<T>() �ŕύX���m�ƒʒm��������
}
```

### RelayCommand
```csharp
// �񓯊��R�}���h�Ή�
public class AsyncRelayCommand : ICommand
{
    // �f�[�^�o�C���h �� �R�}���h���s �� �񓯊�����
}
```

### MainWindowViewModel
- `StatusText`, `RollText`, `YawText` �Ȃǂ̃v���p�e�B
- `ConnectCommand`, `DisconnectCommand`
- `UpdateFromImuData()` �Ńf�[�^�X�V���������f

---

## ?? ���O�o�͗�

### debug_20260126_214611.log
```
[2026-01-26 21:46:11.234] ImuLogger initialized
[2026-01-26 21:46:11.235] Debug log: C:\Users\...\debug_20260126_214611.log
[2026-01-26 21:46:11.236] IMU data log: C:\Users\...\imu_data_20260126_214611.csv
[2026-01-26 21:46:12.102] Starting device connection
[2026-01-26 21:46:12.500] Successfully connected to device
```

### imu_data_20260126_214611.csv
```csv
Timestamp,MessageCounter,Yaw,Pitch,Roll,W,X,Y,Z
12345,100,15.123456,30.456789,45.789012,0.707107,0.707107,0.000000,0.000000
12350,101,16.234567,31.567890,46.890123,0.707107,0.707107,0.000000,0.000000
...
```

---

## ?? �e�X�g���s���@

```bash
# ���ׂẴe�X�g���s
dotnet test

# LandRunnerTest �̂ݎ��s
dotnet test LandRunnerTest

# �ڍ׏o��
dotnet test --verbosity detailed
```

---

## ?? ���s���@

```bash
# �A�v���P�[�V�������s
dotnet run --project LandRunner

# �܂��̓r���h��AEXE�𒼐ڎ��s
LandRunner\bin\Debug\net10.0-windows\LandRunner.exe
```

---

## ?? �t�@�C���\��

```
LandRunner/
������ Models/
��   ������ ImuLogger.cs                 # ���O�ECSV�o��
������ ViewModels/
��   ������ ViewModelBase.cs             # INotifyPropertyChanged
��   ������ RelayCommand.cs              # ICommand����
��   ������ MainWindowViewModel.cs       # �r�W�l�X���W�b�N�E��ԊǗ�
������ Views/ (�܂��� Views �t�H���_)
��   ������ MainWindow.xaml              # UI��`
��   ������ MainWindow.xaml.cs           # CodeBehind
������ ImuLogger.cs                     # �݊����p�i���[�g�j
������ README.md                        # ���̃h�L�������g
������ app.xaml, App.xaml.cs

LandRunnerTest/
������ UnitTest1.cs                     # �e�X�g�X�C�[�g�i10���j
������ LandRunnerTest.csproj
```

---

## ?? MVVM �p�^�[���̗��_

1. **�e�X�g�e�Ր�**: ViewModel �݂̂��e�X�g�\
2. **UI/���W�b�N����**: MainWindow.xaml.cs �������i�R�[�h�r�n�C���h�ŏ����j
3. **�ێ琫����**: �ӔC�����m�ɕ���
4. **�ė��p��**: ViewModel �͕ʂ� View �ł��g�p�\
5. **DataBinding**: �錾�I UI �X�V

---

## ? ����̊g����

1. **�O���t�\��**: �����x�E�p���x�̃��A���^�C���O���t
2. **�L�����u���[�V����**: �Z���T�[�L�����u���[�V�����@�\
3. **�����f�o�C�X**: ���� VITURE �f�o�C�X�̓����ڑ��Ή�
4. **�l�b�g���[�N**: UDP/TCP �ł̃f�[�^���M
5. **�^��E�Đ�**: IMU �f�[�^�̘^��E�Đ��@�\

---

## ?? �Z�p�X�^�b�N

- **Framework**: .NET 10.0
- **UI**: WPF (Windows Presentation Foundation)
- **�p�^�[��**: MVVM (Model-View-ViewModel)
- **�e�X�g**: XUnit 2.9.3
- **�f�o�C�X�ʐM**: GlassBridge�iHID�o�R�j

---

## ?? ���L

- ���O�t�@�C���̓N���A�Ȏ�ŕ����X���b�h����̈��S�ȏ������݂ɑΉ��i`lock` �œ����j
- �t�@�C���n���h���͖����I�Ƀt���b�V�����Ċm���ɃN���[�Y
- �e�X�g��� GC �ɂ��m���ȃ����[�X��҂�

---

**����������**: 2026�N01��26��
**�X�e�[�^�X**: ? �����E�e�X�g���i
