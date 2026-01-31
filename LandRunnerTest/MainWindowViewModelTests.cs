using Microsoft.Extensions.Logging;
using Serilog;
using LandRunner;
using LandRunner.ViewModels;

namespace LandRunnerTest;

/// <summary>
/// MainWindowViewModel のテスト
/// ViewModel のプロパティバインディングと状態管理を検証
/// </summary>
public class MainWindowViewModelTests : IDisposable
{
    private static bool _loggerInitialized = false;

    public MainWindowViewModelTests()
    {
        // テスト用のロギング初期化（1回のみ）
        if (!_loggerInitialized)
        {
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.File(
                    path: Path.Combine(Path.GetTempPath(), $"test_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            App.LoggerFactory = new LoggerFactory()
                .AddSerilog(Log.Logger);

            _loggerInitialized = true;
        }
    }

    public void Dispose()
    {
        Log.CloseAndFlush();
    }

    [Fact]
    public void MainWindowViewModel_Initialize_DefaultValues()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.Equal("Status: Disconnected", viewModel.StatusText);
        Assert.Equal("Messages: 0", viewModel.MessageCounterText);
        Assert.True(viewModel.IsConnectButtonEnabled);
        Assert.False(viewModel.IsDisconnectButtonEnabled);
    }

    [Fact]
    public void MainWindowViewModel_PropertyChanged_RaisesEvent()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        bool eventRaised = false;

        viewModel.PropertyChanged += (s, e) =>
        {
            if (e.PropertyName == nameof(viewModel.StatusText))
                eventRaised = true;
        };

        // Act
        viewModel.StatusText = "Status: Connected";

        // Assert
        Assert.True(eventRaised, "PropertyChanged should be raised");
    }

    [Fact]
    public void MainWindowViewModel_ConnectCommand_IsNotNull()
    {
        // Arrange & Act
        var viewModel = new MainWindowViewModel();

        // Assert
        Assert.NotNull(viewModel.ConnectCommand);
        Assert.NotNull(viewModel.DisconnectCommand);
    }

    [Fact]
    public void MainWindowViewModel_UpdateFromImuData_UpdatesProperties()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var imuData = new GlassBridge.ImuData
        {
            Timestamp = 12345,
            MessageCounter = 100,
            Quaternion = new GlassBridge.Quaternion(0.7071f, 0.7071f, 0.0f, 0.0f),
            EulerAngles = new GlassBridge.EulerAngles(Roll: 45.0f, Pitch: 30.0f, Yaw: 15.0f)
        };

        // Act
        viewModel.UpdateFromImuData(imuData, 1);

        // Assert
        Assert.Contains("45.00", viewModel.RollText);
        Assert.Contains("30.00", viewModel.PitchText);
        Assert.Contains("15.00", viewModel.YawText);
        Assert.Contains("0.707", viewModel.QuatXText);
        Assert.Equal("Messages: 1", viewModel.MessageCounterText);
    }

    [Fact]
    public void MainWindowViewModel_GetLastEulerAngles_ParsesCorrectly()
    {
        // Arrange
        var viewModel = new MainWindowViewModel();
        var imuData = new GlassBridge.ImuData
        {
            Timestamp = 1,
            MessageCounter = 1,
            Quaternion = GlassBridge.Quaternion.Identity,
            EulerAngles = new GlassBridge.EulerAngles(Roll: 25.5f, Pitch: 35.75f, Yaw: 45.25f)
        };
        viewModel.UpdateFromImuData(imuData, 1);

        // Act
        var euler = viewModel.GetLastEulerAngles();

        // Assert
        Assert.NotNull(euler);
        // Allow small parsing tolerance
        Assert.True(Math.Abs(euler.Roll - 25.5f) < 0.1f);
        Assert.True(Math.Abs(euler.Pitch - 35.75f) < 0.1f);
        Assert.True(Math.Abs(euler.Yaw - 45.25f) < 0.1f);
    }
}
