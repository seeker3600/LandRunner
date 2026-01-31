# Copilot ��Ǝw����

LandRunner �v���W�F�N�g�̊J���K�C�h���C���ł��B

---

## 1. C# �R�[�f�B���O�K��

### ��ʓI�ȃK�C�h���C��
- Visual Studio �̃x�X�g�v���N�e�B�X�ɏ]��
- �t�@�C���K�w�Ɩ��O��Ԃ���т�����
- �����̃R�[�h�K��ɍ��킹��

### �v���W�F�N�g�ŗL�̃��[��
- **�֐S�̕���**: ���JAPI�i`GlassBridge` ���O��ԁj�Ɠ��������i`GlassBridge.Internal` ���O��ԁj�������ɕ���
  - ���������t�@�C���� `Internal/` �t�H���_�ɔz�u
  - ���J�C���^�[�t�F�[�X�̂� `ImuDeviceManager`, `IImuDevice` �Ȃ� �����[�g�����ɔz�u
- **VITURE �f�o�C�X�ŗL�̒m��**�� `GlassBridge.Internal.HID.VitureDeviceIdentifiers` �ňꌳ�Ǘ�
  - �x���_�[ID�A�v���_�N�gID �͂��̒萔�N���X���Q��
  - �V�������f���ǉ����͂��̃t�@�C���̂ݏC��
- **�񓯊�����**: `IAsyncDisposable` �����p���A���\�[�X�̎����N���[���A�b�v������

---

## 2. �h�L�������g�\�����[��

### 2.1 �h�L�������g�z�u���[��

**��{����**: �h�L�������g�ʒu�̓���ɂ��A�Q�Ɛ��ƕێ琫�����コ����

| �Ώ� | �z�u�ꏊ | �p�r |
|------|---------|------|
| **README.md** | �\�����[�V�������� | �v���W�F�N�g�S�̂̊T�v�E�Z�b�g�A�b�v�菇 |
| **�v���W�F�N�g README.md** | `GlassBridge/` �Ȃ� | �e�v���W�F�N�g�̊T�v�E�ˑ��֌W�EAPI ���� |
| **ARCHITECTURE.md** | �v���W�F�N�g�t�H���_�i�I�v�V�����j | �A�[�L�e�N�`���݌v�E���W���[���Ӗ� |
| **���̑��h�L�������g** | `docs/` �T�u�t�H���_ | �h���C���m���E�v���g�R���d�l�E�K�C�h |

### 2.2 �����h�L�������g�ꗗ

#### �\�����[�V��������
- **README.md** - �v���W�F�N�g�S�̂̏Љ�E�Z�b�g�A�b�v���@
- **docs/hid/VITURE_Luma.md** - VITURE HID �v���g�R���d�l�i�d�v�j
- **docs/** - �h���C���m���E�v���g�R���d�l�E�����K�C�h

#### �v���W�F�N�g���x���iGlassBridge�j
- **GlassBridge/README.md** - GlassBridge ���JAPI �̐���

#### �h���C���m���E�����K�C�h�idocs/�j
- **docs/hid/VITURE_Luma.md** - VITURE HID �v���g�R���d�l
- **docs/recording/API_GUIDE.md** - IMU �f�[�^�L�^�E�Đ��@�\�̎g�p�K�C�h
- **docs/recording/IMPLEMENTATION.md** - �L�^�@�\�̓�����������

---

## 3. �Q�Ƃ��ׂ��h�L�������g

### HID �v���g�R�������Ɋւ��ꍇ
?? **docs/hid/VITURE_Luma.md**
- VITURE �f�o�C�X�� USB VID/PID
- HID �C���^�[�t�F�[�X�\���iIMU/MCU ��2�̃X�g���[���j
- �v���g�R���d�l�E�p�P�b�g�`��
- �f�o�C�X���ʃ��W�b�N

### GlassBridge API ���g�p����ꍇ
?? **GlassBridge/README.md**
- ���J�C���^�[�t�F�[�X�i`IImuDeviceManager`, `IImuDevice` ���j
- �v���W�F�N�g�\���E�ˑ��֌W

### IMU �f�[�^�L�^�E�Đ��@�\�Ɋւ��ꍇ
?? **docs/recording/API_GUIDE.md**
- `ImuDeviceManager.ConnectAndRecordAsync()` �̎g�p���@
- `ImuDeviceManager.ConnectFromRecordingAsync()` �̎g�p���@
- �L�^�t�@�C���`���iJSON Lines�j

### HidSharp �p�b�P�[�W�Ɋւ��ꍇ
- https://docs.seekye.com/hidsharp/
---

## 4. �R�[�h�i��

### �e�X�g���j
- `GlassBridgeTest/` ���j�b�g�E�����e�X�g������
- �V�@�\�ǉ����̓e�X�g�R�[�h���ꏏ�ɒǉ�
- ���b�N�i`MockHidStreamProvider` ���j�����p���Ď��f�o�C�X�s�v�Ō���

### �p�b�P�[�W�Ǘ�
- `HidSharp` - HID �f�o�C�X�ʐM�i���� 2.6.4�j
- �V�����p�b�P�[�W�ǉ����͌݊����m�F�i���� .NET 10 �Ή��󋵁j

---

## 5. �J�����[�N�t���[

### �V�@�\�ǉ���
1. �Ή�����h�L�������g�iREADME.md, ARCHITECTURE.md ���j���ɍX�V or �쐬
2. �e�X�g�R�[�h �� �����R�[�h �̏��ŊJ��
3. �v���W�F�N�g�\���̈�ѐ����m�F�iInternal/Public �̕����Ȃǁj
4. �r���h�E�e�X�g���i���m�F��ɃR�~�b�g

### �h�L�������g�ǉ���
1. �z�u���[���i��2.1�j�ɏ]���z�u�ꏊ������
2. �����h�L�������g�i��2.2�j�Əd�����Ȃ����m�F

## 6. ���̑����ӓ_
- �}��`���Ƃ��́Amermaid�ł͂Ȃ��A�X�L�[�A�[�g��D�悷�邱�ƁB
- �Ă�񎦂���ۂɒ񎦂���R�[�h�f�Ђ͐��s�ɂƂǂ߂邱�ƁB���邢�̓R�[�h�ł͂Ȃ��}��p���邱�ƁB