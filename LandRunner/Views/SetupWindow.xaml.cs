using System.IO;
using System.Windows;
using System.Windows.Threading;
using GlassBridge;
using LandRunner.Services;
using Microsoft.Extensions.Logging;
using Microsoft.Win32;

namespace LandRunner.Views;

/// <summary>
/// セットアップウィンドウ
/// </summary>
public partial class SetupWindow : Window
{
    private readonly ILogger<SetupWindow> _logger = App.CreateLogger<SetupWindow>();
    private readonly IImuDeviceManager _deviceManager;
    private IImuDevice? _device;
    private CancellationTokenSource? _previewCts;

    public SetupWindow()
    {
        InitializeComponent();
        _deviceManager = new ImuDeviceManager();

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        _logger.LogInformation("セットアップウィンドウを開きました");
        LoadMonitors();
        Log("LandRunner セットアップを開始しました");
        Log("対応デバイス: VITURE Luma, Luma Pro, Pro, One, One Lite");
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _logger.LogInformation("セットアップウィンドウを閉じました");
        _previewCts?.Cancel();
        _device?.DisposeAsync().AsTask().Wait(TimeSpan.FromSeconds(2));
        _deviceManager.Dispose();
    }

    private void LoadMonitors()
    {
        var monitors = MonitorService.GetAllMonitors();

        DisplayMonitorCombo.Items.Clear();
        CaptureMonitorCombo.Items.Clear();

        foreach (var monitor in monitors)
        {
            var displayName = MonitorService.GetDisplayName(monitor);
            DisplayMonitorCombo.Items.Add(new MonitorItem(displayName, monitor));
            CaptureMonitorCombo.Items.Add(new MonitorItem(displayName, monitor));
        }

        // デフォルト選択
        if (monitors.Count >= 2)
        {
            // 2台以上: プライマリをキャプチャ、セカンダリを表示
            var primaryIndex = monitors.ToList().FindIndex(m => m.IsPrimary);
            var secondaryIndex = primaryIndex == 0 ? 1 : 0;

            CaptureMonitorCombo.SelectedIndex = primaryIndex;
            DisplayMonitorCombo.SelectedIndex = secondaryIndex;
        }
        else if (monitors.Count == 1)
        {
            // 1台: 両方同じ（テスト用）
            DisplayMonitorCombo.SelectedIndex = 0;
            CaptureMonitorCombo.SelectedIndex = 0;
            Log("?? モニターが1台のみ検出されました。デバッグモードで動作します。");
        }

        Log($"{monitors.Count} 台のモニターを検出しました");
    }

    private async void ConnectButton_Click(object sender, RoutedEventArgs e)
    {
        ConnectButton.IsEnabled = false;
        DeviceStatusText.Text = "接続中...";
        DeviceStatusText.Foreground = System.Windows.Media.Brushes.Orange;

        try
        {
            // 既存の接続をクリーンアップ
            _previewCts?.Cancel();
            if (_device != null)
            {
                await _device.DisposeAsync();
                _device = null;
            }

            // 接続
            if (PlaybackCheckBox.IsChecked == true)
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "記録フォルダを選択"
                };

                if (dialog.ShowDialog() == true)
                {
                    Log($"記録フォルダ: {dialog.FolderName}");
                    _device = await _deviceManager.ConnectFromRecordingAsync(dialog.FolderName);
                }
            }
            else if (RecordCheckBox.IsChecked == true)
            {
                var dialog = new OpenFolderDialog
                {
                    Title = "記録保存先フォルダを選択"
                };

                if (dialog.ShowDialog() == true)
                {
                    Log($"記録保存先: {dialog.FolderName}");
                    _device = await _deviceManager.ConnectAndRecordAsync(dialog.FolderName);
                }
            }
            else
            {
                _device = await _deviceManager.ConnectAsync();
            }

            if (_device != null)
            {
                DeviceStatusText.Text = "接続済み ?";
                DeviceStatusText.Foreground = System.Windows.Media.Brushes.Green;
                StartButton.IsEnabled = true;
                _logger.LogInformation("XRデバイスに接続しました");
                Log("XRデバイスに接続しました");

                // プレビュー開始
                StartImuPreview();
            }
            else
            {
                DeviceStatusText.Text = "デバイスが見つかりません";
                DeviceStatusText.Foreground = System.Windows.Media.Brushes.Red;
                _logger.LogWarning("XRデバイスが見つかりませんでした");
                Log("? XRデバイスが見つかりませんでした");
            }
        }
        catch (Exception ex)
        {
            DeviceStatusText.Text = "接続エラー";
            DeviceStatusText.Foreground = System.Windows.Media.Brushes.Red;
            _logger.LogError(ex, "デバイス接続エラー");
            Log($"? 接続エラー: {ex.Message}");
        }
        finally
        {
            ConnectButton.IsEnabled = true;
            ConnectButton.Content = _device != null ? "再接続" : "接続";
        }
    }

    private async void StartImuPreview()
    {
        if (_device == null) return;

        _previewCts = new CancellationTokenSource();
        var token = _previewCts.Token;

        try
        {
            await foreach (var data in _device.GetImuDataStreamAsync(token))
            {
                var euler = data.EulerAngles;
                Dispatcher.Invoke(() =>
                {
                    ImuDataText.Text = $"Roll: {euler.Roll,7:F1}°  Pitch: {euler.Pitch,7:F1}°  Yaw: {euler.Yaw,7:F1}°";
                });
            }
        }
        catch (OperationCanceledException)
        {
            // 正常なキャンセル
        }
    }

    private void PlaybackCheckBox_Checked(object sender, RoutedEventArgs e)
    {
        if (RecordCheckBox != null)
            RecordCheckBox.IsEnabled = false;
    }

    private void PlaybackCheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        if (RecordCheckBox != null)
            RecordCheckBox.IsEnabled = true;
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (_device == null)
        {
            Log("? デバイスが接続されていません");
            return;
        }

        var displayMonitor = (DisplayMonitorCombo.SelectedItem as MonitorItem)?.Monitor;
        var captureMonitor = (CaptureMonitorCombo.SelectedItem as MonitorItem)?.Monitor;

        if (displayMonitor == null || captureMonitor == null)
        {
            Log("? モニターを選択してください");
            return;
        }

        // IMU プレビューを停止（デバイスは DisplayWindow に引き渡す）
        _previewCts?.Cancel();

        Log("表示ウィンドウを起動します...");

        // 表示ウィンドウを開く
        var displayWindow = new DisplayWindow(_device, displayMonitor, captureMonitor);
        _device = null; // 所有権を移譲
        displayWindow.Show();

        // セットアップウィンドウを閉じる
        Close();
    }

    private void Log(string message)
    {
        var timestamp = DateTime.Now.ToString("HH:mm:ss");
        LogTextBox.AppendText($"[{timestamp}] {message}\n");
        LogTextBox.ScrollToEnd();
    }

    private record MonitorItem(string DisplayName, MonitorInfo Monitor)
    {
        public override string ToString() => DisplayName;
    }
}
