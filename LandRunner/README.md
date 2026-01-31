# LandRunner - VITURE Luma IMU �r���[�A

WPF �Ŏ������ꂽ VITURE Luma �����̃��A���^�C�� IMU �f�[�^�r���[�A�E���K�[�ł��B

## ��ȋ@�\

- ?? **���A���^�C�� IMU �f�[�^�\��**�FEuler �p�x�AQuaternion ���_�b�V���{�[�h�ŕ\��
- ?? **3D ��]�r�W���A���C�[�[�V����**�FXYZ ���� Yaw �p�Ɋ�Â��ĉ�]��������Ԃŕ\��
- ?? **IMU �f�[�^�����L�^**�FGlassBridge �̋L�^�@�\�����p���� IMU �f�[�^�� JSON Lines �`���ŋL�^
- ?? **�f�o�b�O ���O�o��**�F���ׂẴA�N�e�B�r�e�B���^�C���X�^���v�t�����O�t�@�C���ɋL�^

## �v���W�F�N�g�\���iMVVM �p�^�[���j

```
LandRunner/
������ Models/
��   ������ DebugLogger.cs            # �^�C���X�^���v�t���f�o�b�O���O�o��
��   ������ ImuLogger.cs              # DebugLogger �̃G�C���A�X/���b�p�[
������ ViewModels/
��   ������ ViewModelBase.cs          # MVVM ��{�N���X�iINotifyPropertyChanged�j
��   ������ RelayCommand.cs           # ICommand �����i�����E�񓯊��Ή��j
��   ������ MainWindowViewModel.cs    # ��ԊǗ��E�r�W�l�X���W�b�N
������ Views/
��   ������ MainWindow.xaml           # UI ���C�A�E�g�iDataBinding�j
��   ������ MainWindow.xaml.cs        # CodeBehind�i�r�W���A�����̂݁j
������ App.xaml
������ App.xaml.cs
������ GlassBridge ����
    ������ ConnectAndRecordAsync() �� JSON Lines �L�^
```

### �e�R���|�[�l���g�̖���

| �t�@�C�� | ���� | ���� |
|---------|------|------|
| **ViewModelBase.cs** | ��{�N���X | `INotifyPropertyChanged` �����A�v���p�e�B�ύX�ʒm |
| **RelayCommand.cs** | �R�}���h���� | UI �{�^���E���j���[����̃n���h�����O�i�񓯊��Ή��j |
| **MainWindowViewModel.cs** | ViewModel | UI ��ԁEGlassBridge �̐ڑ��E�f�[�^�X�V���Ǘ� |
| **DebugLogger.cs** | ���O�o�� | �t�@�C���E�R���\�[���ւ̃^�C���X�^���v�t�����O |
| **MainWindow.xaml** | View | UI ��`�iMVVM DataBinding�j |

## ���s�E�e�X�g

### �A�v���P�[�V�������s

```bash
# �f�o�b�O�r���h�����s
dotnet run --project LandRunner

# �����[�X�r���h�����s
dotnet run --project LandRunner --configuration Release
```

### �e�X�g���s

```bash
# �S�e�X�g���s
dotnet test LandRunnerTest

# ����̃e�X�g�N���X�̂�
dotnet test LandRunnerTest --filter "FullyQualifiedName~ImuLoggerTests"

# �ڍ׏o��
dotnet test LandRunnerTest --verbosity detailed
```

## ���O�o�͐�

### �f�B���N�g���\��

```
%APPDATA%\LandRunner\                      �i��FC:\Users\<User>\AppData\Roaming\LandRunner\�j
������ debug_<yyyyMMdd_HHmmss>.log            �f�o�b�O���O�i�^�C���X�^���v�t���j
������ imu_data_<yyyyMMdd_HHmmss>.jsonl       IMU �f�[�^�L�^�iGlassBridge �����������j
```

### �f�o�b�O���O�`��

�e���O�G���g���̓^�C���X�^���v�Ƌ��ɏo�͂���܂��F

```
[2026-01-26 21:46:11.234] ImuLogger initialized
[2026-01-26 21:46:11.235] Debug log: C:\Users\...\AppData\Roaming\LandRunner\debug_20260126_214611.log
[2026-01-26 21:46:11.236] Recording IMU data to: C:\Users\...\AppData\Roaming\LandRunner
[2026-01-26 21:46:12.500] Successfully connected to device
[2026-01-26 21:46:13.100] Received IMU frame: Timestamp=12345, Roll=45.0��
[2026-01-26 21:46:15.800] Disposing device (GlassBridge will finalize recording)
```

### IMU �f�[�^�L�^�`��

GlassBridge �� `ConnectAndRecordAsync()` �ɂ��AIMU �f�[�^�� **JSON Lines �`��** �Ŏ����L�^����܂��B�e�s��1�̃t���[���ł��F

```json
{"Timestamp":12345,"MessageCounter":100,"Quaternion":{"W":0.707107,"X":0.707107,"Y":0,"Z":0},"EulerAngles":{"Roll":45.0,"Pitch":30.0,"Yaw":15.0}}
{"Timestamp":12350,"MessageCounter":101,"Quaternion":{"W":0.707107,"X":0.707107,"Y":0,"Z":0},"EulerAngles":{"Roll":46.0,"Pitch":31.0,"Yaw":16.0}}
```

�ڍׂ� **GlassBridge/RECORDING_API_GUIDE.md** ���Q�Ƃ��Ă��������B

## �e�X�g

LandRunnerTest �v���W�F�N�g�ɂ́A�ȉ��̃e�X�g�N���X�E�e�X�g�P�[�X���܂܂�Ă��܂��B

### �e�X�g�N���X�ꗗ�i�S 19 ���j

| �e�X�g�N���X | �ΏۃN���X | �e�X�g�� | ���� |
|----------|-----------|--------|------|
| **ImuLoggerTests** | ImuLogger | 4 �� | �f�o�b�O���O�o�͋@�\�E�X���b�h���S�� |
| **ImuDeviceManagerTests** | ImuDeviceManager | 1 �� | �f�o�C�X�}�l�[�W���[�̃C���X�^���X�� |
| **MockImuDeviceTests** | MockImuDevice | 1 �� | ���b�N�f�o�C�X�̃X�g���[������ |
| **ImuDataTests** | ImuData | 3 �� | IMU �f�[�^�\���E�l�̐��x |
| **MainWindowViewModelTests** | MainWindowViewModel | 5 �� | ViewModel ��ԊǗ��E�C�x���g |
| **RelayCommandTests** | RelayCommand | 5 �� | �R�}���h���s�E�񓯊��Ή� |
| **���v** | | **19 ��** | **���ׂč��i ?** |

### �e�X�g�P�[�X�ڍ�

#### ImuLoggerTests
- `ImuLogger_Initialize_CreatesLogFile` - ���O�t�@�C���̍쐬���m�F
- `ImuLogger_LogDebug_WritesMessage` - ���b�Z�[�W���t�@�C���ɋL�^����邱�Ƃ��m�F
- `ImuLogger_Dispose_ClosesFiles` - �j�����Ƀt�@�C�����N���[�Y����邱�Ƃ��m�F
- `ImuLogger_ThreadSafe_ConcurrentWrites` - �}���`�X���b�h���ł̈��S�����m�F

#### ImuDeviceManagerTests
- `ImuDeviceManager_CreateInstance_ShouldNotThrow` - �C���X�^���X�����������邱�Ƃ��m�F

#### MockImuDeviceTests
- `MockDevice_StreamData_ProducesData` - ���b�N �f�o�C�X���f�[�^�𐶐����邱�Ƃ��m�F

#### ImuDataTests
- `ImuData_EulerAngles_ShouldBeAccurate` - Euler �p�x�v�Z�̐��x���m�F
- `Quaternion_Operations_ShouldWork` - Quaternion ���Z���m�F
- `ImuData_Record_ShouldContainRequiredFields` - ���R�[�h�^�ɕK�{�t�B�[���h���܂܂�邱�Ƃ��m�F

#### MainWindowViewModelTests
- `MainWindowViewModel_Initialize_DefaultValues` - �����l���������ݒ肳��邱�Ƃ��m�F
- `MainWindowViewModel_PropertyChanged_RaisesEvent` - �v���p�e�B�ύX���ɃC�x���g�����΂��邱�Ƃ��m�F
- `MainWindowViewModel_ConnectCommand_IsNotNull` - ConnectCommand �� null �łȂ����Ƃ��m�F
- `MainWindowViewModel_UpdateFromImuData_UpdatesProperties` - IMU �f�[�^�X�V���� UI �����f����邱�Ƃ��m�F
- `MainWindowViewModel_GetLastEulerAngles_ParsesCorrectly` - Euler �p�x�̉�͐��x���m�F

#### RelayCommandTests
- `RelayCommand_Execute_InvokesAction` - �R�}���h���s���A�N�V�������Ăяo�����Ƃ��m�F
- `RelayCommand_CanExecute_ReturnsTrue_WhenNoCondition` - �����Ȃ��Ŏ��s�\�ł��邱�Ƃ��m�F
- `RelayCommand_CanExecute_RespectsPredicate` - �q�ꃍ�W�b�N���������@�\���邱�Ƃ��m�F
- `AsyncRelayCommand_Execute_InvokesAsyncAction` - �񓯊��R�}���h���s���m�F
- `AsyncRelayCommand_CanExecute_ReturnsTrue_WhenNotExecuting` - �񓯊����s���łȂ����͎��s�\�ł��邱�Ƃ��m�F

## �ˑ��֌W�ƋZ�p�X�^�b�N

| ���� | ���� |
|------|------|
| **�t���[�����[�N** | .NET 10.0-windows7.0 |
| **UI �t���[�����[�N** | WPF (Windows Presentation Foundation) |
| **IMU ���C�u����** | GlassBridge (���̃\�����[�V������) |
| **�e�X�g�t���[�����[�N** | xUnit + �W�����j�b�g�e�X�g |
| **�A�[�L�e�N�`��** | MVVM (Model-View-ViewModel) |

## MVVM �p�^�[���̐݌v

LandRunner �� MVVM �p�^�[�����̗p���A�֐S�̕������������Ă��܂��F

### �������S

| ���C���[ | �S�� | ��v�N���X |
|--------|------|-----------|
| **Model** | �f�[�^�E�r�W�l�X���W�b�N | DebugLogger�AImuData |
| **ViewModel** | ��ԊǗ��E�R�}���h���� | MainWindowViewModel�ARelayCommand |
| **View** | UI �\�� | MainWindow.xaml |

### �ʐM�t���[

```
User �� View (XAML) 
          ��
       MainWindow.xaml.cs (�ŏ���)
          ��
       ViewModel (MainWindowViewModel)
          ��
       GlassBridge (IMU �擾�E�L�^)
          ��
       Model (DebugLogger�AImuData)
```

### DataBinding �̊��p

ViewModel�̃v���p�e�B�ύX�� XAML DataBinding �Ŏ������o�F

```xml
<TextBlock Text="{Binding RollText}" />
<TextBlock Text="{Binding PitchText}" />
<TextBlock Text="{Binding YawText}" />
```

�ڍׂ� `MainWindow.xaml` ���Q�ƁB

## GlassBridge �Ƃ̓���

### �]���̃A�v���[�`
```
LandRunner �� IMU �f�[�^��M �� �蓮�� CSV �L�^
```

### ������̃A�v���[�`
```
ConnectAndRecordAsync()
    ��
GlassBridge �������L�^ �� imu_data_*.jsonl
    ��
LandRunner �͕\���ƃ��O�o�݂͂̂ɓ���
```

�����b�g�F
- ?? LandRunner ���Ȍ��ɂȂ�i�\���E���O�̂݁j
- ?? GlassBridge ���W�������� JSON Lines �`���ŋL�^
- ?? �Đ��@�\�iConnectFromRecordingAsync()�j�ŊȒP�Ƀ��v���C

�ڍׂ� **GlassBridge/RECORDING_API_GUIDE.md** ���Q�ƁB

