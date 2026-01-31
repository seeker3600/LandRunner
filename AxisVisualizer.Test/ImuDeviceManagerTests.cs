using GlassBridge;

namespace LandRunnerTest;

/// <summary>
/// IImuDeviceManager と実装のテスト
/// デバイス接続・初期化を検証
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
