using System;
using System.Windows.Input;
using Microsoft.Extensions.Logging;
using GlassBridge;

namespace LandRunner.ViewModels;

/// <summary>
/// IMUデータ表示用のViewModel
/// GlassBridgeの ConnectAndRecordAsync() を使用してデータを自動録音
/// </summary>
public class MainWindowViewModel : ViewModelBase
{
    private string _statusText = "Status: Disconnected";
    private string _messageCounterText = "Messages: 0";
    private string _lastTimestampText = "Timestamp: 0";
    private string _rollText = "Roll:  0.00°";
    private string _pitchText = "Pitch: 0.00°";
    private string _yawText = "Yaw:   0.00°";
    private string _quatWText = "W: 1.000";
    private string _quatXText = "X: 0.000";
    private string _quatYText = "Y: 0.000";
    private string _quatZText = "Z: 0.000";
    private string _timestampValueText = "Timestamp: 0";
    private string _counterValueText = "Counter: 0";
    private string _infoText = "Click 'Connect Device' to start";
    private string _visualizationLabelText = "Waiting for data...";
    private bool _isConnectButtonEnabled = true;
    private bool _isDisconnectButtonEnabled = false;

    // Display properties
    public string StatusText
    {
        get => _statusText;
        set => SetProperty(ref _statusText, value);
    }

    public string MessageCounterText
    {
        get => _messageCounterText;
        set => SetProperty(ref _messageCounterText, value);
    }

    public string LastTimestampText
    {
        get => _lastTimestampText;
        set => SetProperty(ref _lastTimestampText, value);
    }

    public string RollText
    {
        get => _rollText;
        set => SetProperty(ref _rollText, value);
    }

    public string PitchText
    {
        get => _pitchText;
        set => SetProperty(ref _pitchText, value);
    }

    public string YawText
    {
        get => _yawText;
        set => SetProperty(ref _yawText, value);
    }

    public string QuatWText
    {
        get => _quatWText;
        set => SetProperty(ref _quatWText, value);
    }

    public string QuatXText
    {
        get => _quatXText;
        set => SetProperty(ref _quatXText, value);
    }

    public string QuatYText
    {
        get => _quatYText;
        set => SetProperty(ref _quatYText, value);
    }

    public string QuatZText
    {
        get => _quatZText;
        set => SetProperty(ref _quatZText, value);
    }

    public string TimestampValueText
    {
        get => _timestampValueText;
        set => SetProperty(ref _timestampValueText, value);
    }

    public string CounterValueText
    {
        get => _counterValueText;
        set => SetProperty(ref _counterValueText, value);
    }

    public string InfoText
    {
        get => _infoText;
        set => SetProperty(ref _infoText, value);
    }

    public string VisualizationLabelText
    {
        get => _visualizationLabelText;
        set => SetProperty(ref _visualizationLabelText, value);
    }

    public bool IsConnectButtonEnabled
    {
        get => _isConnectButtonEnabled;
        set => SetProperty(ref _isConnectButtonEnabled, value);
    }

    public bool IsDisconnectButtonEnabled
    {
        get => _isDisconnectButtonEnabled;
        set => SetProperty(ref _isDisconnectButtonEnabled, value);
    }

    // Commands
    public ICommand ConnectCommand { get; }
    public ICommand DisconnectCommand { get; }

    // Internal fields
    private IImuDeviceManager? _deviceManager;
    private IImuDevice? _device;
    private CancellationTokenSource? _cancellationTokenSource;
    private readonly ILogger<MainWindowViewModel> _logger;

    public MainWindowViewModel()
    {
        _logger = App.CreateLogger<MainWindowViewModel>();
        ConnectCommand = new AsyncRelayCommand(ConnectAsync, () => IsConnectButtonEnabled);
        DisconnectCommand = new AsyncRelayCommand(DisconnectAsync, () => IsDisconnectButtonEnabled);
    }

    public async Task ConnectAsync()
    {
        try
        {
            IsConnectButtonEnabled = false;
            InfoText = "Connecting to device...";

            var appDataPath = System.IO.Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LandRunner");
            System.IO.Directory.CreateDirectory(appDataPath);

            _logger.LogDebug("Starting device connection with GlassBridge recording");

            _deviceManager = new ImuDeviceManager();
            
            // GlassBridgeの ConnectAndRecordAsync() を使用してデータを自動記録
            _device = await _deviceManager.ConnectAndRecordAsync(appDataPath);

            if (_device == null)
            {
                InfoText = "Failed to connect to device";
                _logger.LogError("Failed to connect to VITURE Luma device");
                IsConnectButtonEnabled = true;
                return;
            }

            StatusText = "Status: Connected";
            IsDisconnectButtonEnabled = true;
            IsConnectButtonEnabled = false;
            InfoText = "Connected! Receiving and recording data...";
            VisualizationLabelText = "";

            _logger.LogInformation("Successfully connected to device");
            _logger.LogInformation("Recording IMU data to: {AppDataPath}", appDataPath);

            _cancellationTokenSource = new CancellationTokenSource();
            _ = StreamDataAsync(_cancellationTokenSource.Token);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Connection error: {ErrorMessage}", ex.Message);
            InfoText = $"Error: {ex.Message}";
            IsConnectButtonEnabled = true;
        }
    }

    public async Task DisconnectAsync()
    {
        try
        {
            _cancellationTokenSource?.Cancel();
            _cancellationTokenSource?.Dispose();

            if (_device != null)
            {
                _logger.LogDebug("Disposing device (GlassBridge will finalize recording)");
                await _device.DisposeAsync();
            }

            _deviceManager?.Dispose();

            StatusText = "Status: Disconnected";
            IsDisconnectButtonEnabled = false;
            IsConnectButtonEnabled = true;
            InfoText = "Disconnected. Click 'Connect Device' to start.";
            VisualizationLabelText = "Waiting for data...";

            _logger.LogInformation("Device disconnected");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Disconnect error: {ErrorMessage}", ex.Message);
            InfoText = $"Disconnect error: {ex.Message}";
        }
    }

    private async Task StreamDataAsync(CancellationToken cancellationToken)
    {
        if (_device == null) return;

        try
        {
            var count = 0;
            await foreach (var imuData in _device.GetImuDataStreamAsync(cancellationToken))
            {
                count++;
                
                // UIの更新はメインスレッドで実行される必要があります
                // ViewModelBaseの仕組みでUIが更新されます
                UpdateFromImuData(imuData, count);
            }
        }
        catch (OperationCanceledException)
        {
            _logger.LogDebug("Data streaming cancelled");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Streaming error: {ErrorMessage}", ex.Message);
            InfoText = $"Streaming error: {ex.Message}";
        }
    }

    public void UpdateFromImuData(ImuData data, int messageCount)
    {
        try
        {
            MessageCounterText = $"Messages: {messageCount}";
            LastTimestampText = $"Timestamp: {data.Timestamp}";

            var euler = data.EulerAngles;
            RollText = $"Roll:  {euler.Roll:F2}°";
            PitchText = $"Pitch: {euler.Pitch:F2}°";
            YawText = $"Yaw:   {euler.Yaw:F2}°";

            var quat = data.Quaternion;
            QuatWText = $"W: {quat.W:F3}";
            QuatXText = $"X: {quat.X:F3}";
            QuatYText = $"Y: {quat.Y:F3}";
            QuatZText = $"Z: {quat.Z:F3}";

            TimestampValueText = $"Timestamp: {data.Timestamp}";
            CounterValueText = $"Counter: {data.MessageCounter}";
        }
        catch (Exception ex)
        {
            _logger.LogTrace(ex, "UI update error: {ErrorMessage}", ex.Message);
        }
    }

    public EulerAngles GetLastEulerAngles()
    {
        // Parse from text fields
        float roll = float.Parse(RollText.Replace("Roll:  ", "").Replace("°", ""));
        float pitch = float.Parse(PitchText.Replace("Pitch: ", "").Replace("°", ""));
        float yaw = float.Parse(YawText.Replace("Yaw:   ", "").Replace("°", ""));
        return new EulerAngles(roll, pitch, yaw);
    }
}
