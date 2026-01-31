# LandRunner

LandRunner �́AVITURE XR �O���X���z�X�g OS ���琧�䂷�邽�߂� .NET ���C�u��������� WPF �A�v���P�[�V�����ł��B

## �T�v

### ?? GlassBridge ���C�u����
VITURE �f�o�C�X�iLuma�ALuma Pro�APro�AOne�AOne Lite �Ȃǁj�� USB HID �C���^�[�t�F�[�X�o�R�ŒʐM���A3DoF �� IMU�i�p���j�f�[�^���擾�E�L�^�E�Đ����܂��B�e�X�g�p�̃��b�N�������܂܂�Ă���A�{�̃f�o�C�X���Ȃ��Ă��J���E���؂��\�ł��B

### ?? LandRunner WPF �A�v���P�[�V����
GlassBridge ���g�p�����AVITURE Luma �����̃��A���^�C�� IMU �f�[�^�r���[�A�E���K�[�BEuler �p�EQuaternion �̕\���A3D ��]�r�W���A���C�[�[�V�����A�f�o�b�O���O�o�͂ɑΉ����Ă��܂��B

## �Z�b�g�A�b�v

### �K�v�Ȋ�
- **.NET 10** �ȏ�
- **Visual Studio 2022** �ȏ�i�����j
- **Windows**�iUSB HID �ʐM�̂��߁j

### �C���X�g�[���E�r���h

1. ���|�W�g�����N���[���F
   ```bash
   git clone https://github.com/seeker3600/LandRunner.git
   cd LandRunner
   ```

2. �v���W�F�N�g���r���h�F
   ```bash
   dotnet build
   ```

3. �e�X�g�����s�F
   ```bash
   dotnet test
   ```

4. LandRunner �A�v�������s�iWindows �̂݁j�F
   ```bash
   dotnet run --project LandRunner
   ```

## �v���W�F�N�g�\��

| �v���W�F�N�g | ���� | �^�[�Q�b�g�t���[�����[�N |
|-----------|------|----------------------|
| **GlassBridge** | ���J API�iImuDeviceManager�AImuData �Ȃǁj | net10.0-windows7.0 |
| **GlassBridgeTest** | ���j�b�g�E�����e�X�g | net10.0-windows7.0 |
| **LandRunner** | WPF IMU �r���[�A�E���K�[ | net10.0-windows7.0 |
| **LandRunnerTest** | LandRunner �̃e�X�g | net10.0-windows7.0 |

## ?? �h�L�������g�\���ƎQ�ƃK�C�h

### ?? �Q�Ɛ�ʃK�C�h

#### HID �v���g�R���d�l���m�F����
�� **docs/hid/VITURE_Luma.md**
- VITURE �f�o�C�X�� USB Vendor ID / Product ID
- HID �C���^�[�t�F�[�X�iIMU/MCU�j�̍\���E�p�P�b�g�\��
- �v���g�R���d�l�E����R�}���h
- �f�o�C�X�ʂ̌݊������

#### GlassBridge API ���g�p����
�� **GlassBridge/README.md**
- ���J�C���^�[�t�F�[�X�iIImuDeviceManager�AIImuDevice �Ȃǁj
- �v���W�F�N�g�\���E�t�H���_�z�u
- �N�C�b�N�X�^�[�g��E��{�I�Ȏg����

#### IMU �f�[�^�L�^�E�Đ��@�\�̏ڍ�
�� **GlassBridge/RECORDING_API_GUIDE.md**
- `ConnectAndRecordAsync()` �̎g�p���@
- `ConnectFromRecordingAsync()` �̎g�p���@
- �L�^�t�@�C���`���iJSON Lines�j�ڍ�

#### LandRunner �A�v���P�[�V����
�� **LandRunner/README.md**
- �@�\�T�v�i���A���^�C���\���A3D �r�W���A���C�[�[�V�����A���M���O�j
- MVVM �A�[�L�e�N�`���E�t�H���_�\��
- ���s���@�E���O�o�͐�
- �e�X�g�ꗗ�E�e�X�g�P�[�X

### ?? �h�L�������g�z�u���[��

| �h�L�������g | �z�u�� | �p�r |
|-----------|-------|------|
| **README.md** | �\�����[�V�������[�g | �v���W�F�N�g�S�̂̊T�v�E�Z�b�g�A�b�v�E�h�L�������g�K�C�h |
| **GlassBridge/README.md** | `GlassBridge/` | GlassBridge �̌��J API �d�l�E�g�p�� |
| **GlassBridge/RECORDING_API_GUIDE.md** | `GlassBridge/` | �L�^�E�Đ��@�\�̏ڍ׃K�C�h |
| **LandRunner/README.md** | `LandRunner/` | LandRunner �A�v���̐����E�@�\�E���M���O��� |
| **docs/hid/VITURE_Luma.md** | `docs/hid/` | VITURE HID �v���g�R���d�l�i��{�����j|
| **���̑��h�L�������g** | `docs/` �T�u�t�H���_ | �h���C�o�[���E�v���g�R���ڍׁE�����K�C�h |

## ?? �R�[�h�Ǘ����j

### �J�����̃��[��
1. **�e�X�g�R�[�h** - �V�@�\��ǉ�����ꍇ�́A�e�X�g�R�[�h�����킹�Ď���
2. **�R�[�f�B���O�K��** - `.github/copilot-instructions.md` �ɏ]��
3. **�h�L�������g** - �V�@�\�ǉ����͑Ή����� README.md �� ARCHITECTURE.md ���X�V
4. **���W���[���\��** - GlassBridge �ł́A���J API�i�����j�Ɠ��������iInternal/�j�𖾊m�ɕ���

### �p�b�P�[�W�Ǘ�
- **HidSharp** 2.6.4 - USB HID �f�o�C�X�ʐM
- �V�����p�b�P�[�W�ǉ����� .NET 10 �݊������m�F

## ?? ���ݎQ�Ɓi�Z�p�Ҍ����j

- **GlassBridge �� HID �჌�x��������ύX����** �� docs/hid/VITURE_Luma.md ���Q�Ƃ��Ďd�l���m�F
- **VITURE �f�o�C�X�̑Ή��󋵂��m�F����** �� docs/hid/VITURE_Luma.md�i��2�́j���m�F
- **LandRunner �� GlassBridge ���g�p����** �� GlassBridge/README.md ���Q��
- **�V�����L�^�`����ǉ�����** �� GlassBridge/RECORDING_API_GUIDE.md ���Q��

## ���C�Z���X

�ڍׂ̓��|�W�g�����Q�Ƃ��Ă��������B