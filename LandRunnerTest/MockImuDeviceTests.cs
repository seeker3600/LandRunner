using GlassBridge;

namespace LandRunnerTest;

/// <summary>
/// MockImuDevice のテスト
/// モックデバイスのデータストリーミングを検証
/// </summary>
public class MockImuDeviceTests
{
    [Fact]
    public async Task MockDevice_StreamData_ProducesData()
    {
        // Arrange
        var mockDevice = MockImuDevice.CreateWithPeriodicData(
            counter =>
            {
                float angle = counter * 5.0f;
                return new ImuData
                {
                    Quaternion = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f),
                    EulerAngles = new EulerAngles(angle, angle * 0.5f, angle * 1.5f),
                    Timestamp = (uint)counter,
                    MessageCounter = (ushort)counter
                };
            },
            intervalMs: 10,
            maxIterations: 10
        );

        // Act
        var dataPoints = new List<ImuData>();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));
        
        await foreach (var data in mockDevice.GetImuDataStreamAsync(cts.Token))
        {
            dataPoints.Add(data);
            if (dataPoints.Count >= 5)
                break;
        }

        // Assert
        Assert.NotEmpty(dataPoints);
        Assert.True(dataPoints.Count >= 5, "Should have received at least 5 data points");
        Assert.Equal(0, dataPoints[0].MessageCounter);
        Assert.True(dataPoints[1].MessageCounter > dataPoints[0].MessageCounter, "Message counter should increment");
    }
}
