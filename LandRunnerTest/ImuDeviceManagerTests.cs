using GlassBridge;

namespace LandRunnerTest;

/// <summary>
/// IImuDeviceManager �Ǝ����̃e�X�g
/// �f�o�C�X�ڑ��E������������
/// </summary>
public class ImuDeviceManagerTests
{
    [Fact]
    public void ImuDeviceManager_CreateInstance_ShouldNotThrow()
    {
        // Arrange & Act & Assert
        using var manager = new ImuDeviceManager();
        Assert.NotNull(manager);
    }
}
