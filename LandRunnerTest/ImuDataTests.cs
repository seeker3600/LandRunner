using GlassBridge;

namespace LandRunnerTest;

/// <summary>
/// ImuData �̃e�X�g
/// Euler�p�x�EQuaternion�̃f�[�^����
/// </summary>
public class ImuDataTests
{
    [Fact]
    public void ImuData_EulerAngles_ShouldBeAccurate()
    {
        // Arrange
        var eulerAngles = new EulerAngles(Roll: 12.5f, Pitch: 45.0f, Yaw: -30.5f);
        var data = new ImuData
        {
            Timestamp = 1,
            MessageCounter = 1,
            Quaternion = Quaternion.Identity,
            EulerAngles = eulerAngles
        };

        // Act & Assert
        Assert.Equal(12.5f, data.EulerAngles.Roll);
        Assert.Equal(45.0f, data.EulerAngles.Pitch);
        Assert.Equal(-30.5f, data.EulerAngles.Yaw);
    }

    [Fact]
    public void Quaternion_Operations_ShouldWork()
    {
        // Arrange
        var q1 = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f);
        var q2 = new Quaternion(0.7071f, 0.7071f, 0.0f, 0.0f);

        // Act
        var conjugate = q1.Conjugate();
        var product = q1 * q2;

        // Assert
        Assert.Equal(1.0f, conjugate.W);
        Assert.Equal(0.0f, conjugate.X);
        Assert.NotNull(product);
    }

    [Fact]
    public void ImuData_Record_ShouldContainRequiredFields()
    {
        // Arrange
        var data = new ImuData
        {
            Timestamp = 12345,
            MessageCounter = 100,
            Quaternion = new Quaternion(0.7071f, 0.7071f, 0.0f, 0.0f),
            EulerAngles = new EulerAngles(Roll: 45.0f, Pitch: 30.0f, Yaw: 15.0f)
        };

        // Act & Assert
        Assert.Equal(12345u, data.Timestamp);
        Assert.Equal(100, data.MessageCounter);
        Assert.NotNull(data.Quaternion);
        Assert.NotNull(data.EulerAngles);
    }
}
