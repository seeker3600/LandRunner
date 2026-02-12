using Microsoft.UI.Composition;
using Microsoft.UI.Xaml;
using Microsoft.UI.Xaml.Controls;
using Microsoft.UI.Xaml.Hosting;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using Windows.Win32;
using Windows.Win32.Foundation;

namespace WinUiApp1;

public sealed partial class MainWindow : Window
{
    private ScreenCaptureService? _captureService;
    private ReadOnlyCollection<DisplayMonitorInfo> _availableDisplays = ReadOnlyCollection<DisplayMonitorInfo>.Empty;

    public MainWindow()
    {
        InitializeComponent();
        LoadDisplays();
        SetWindowCaptureProtection();
    }

    private void SetWindowCaptureProtection()
    {
        var hwnd = WinRT.Interop.WindowNative.GetWindowHandle(this);
        PInvoke.SetWindowDisplayAffinity(new HWND(hwnd), Windows.Win32.UI.WindowsAndMessaging.WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE);
    }

    private void LoadDisplays()
    {
        _availableDisplays = MonitorService.GetAllMonitors();
        DisplayComboBox.ItemsSource = _availableDisplays;
        if (_availableDisplays.Count != 0)
        {
            DisplayComboBox.SelectedIndex = 0;
        }
    }

    private void DisplayComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        StartButton.IsEnabled = DisplayComboBox.SelectedItem != null;
    }

    private async void StartButton_Click(object sender, RoutedEventArgs e)
    {
        if (DisplayComboBox.SelectedItem is not DisplayMonitorInfo selectedDisplay)
            return;

        _captureService?.Dispose();
        _captureService = new ScreenCaptureService();
        _captureService.VisualCreated += OnCaptureVisualCreated;
        _captureService.StatsUpdated += OnCaptureStatsUpdated;

        var compositor = ElementCompositionPreview.GetElementVisual(CaptureContainer).Compositor;

        if (await _captureService.StartCaptureAsync(selectedDisplay, compositor))
        {
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
            DisplayComboBox.IsEnabled = false;
        }
        else
        {
            _captureService?.Dispose();
            _captureService = null;

            var dialog = new ContentDialog
            {
                Title = "Capture Failed",
                Content = "Failed to start screen capture. Please try another display.",
                CloseButtonText = "OK",
                XamlRoot = Content.XamlRoot
            };
            await dialog.ShowAsync();
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _captureService?.Stop();
        ElementCompositionPreview.SetElementChildVisual(CaptureContainer, null);

        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
        DisplayComboBox.IsEnabled = true;
        StatsTextBlock.Text = "No stats";
    }

    private void OnCaptureVisualCreated(object? sender, SpriteVisual visual)
    {
        DispatcherQueue.TryEnqueue(() =>
        {
            ElementCompositionPreview.SetElementChildVisual(CaptureContainer, visual);
        });
    }

    private void OnCaptureStatsUpdated(object? sender, CaptureStats stats)
    {
        // B-2: 低優先度キューで UI スレッド負荷を軽減
        DispatcherQueue.TryEnqueue(Microsoft.UI.Dispatching.DispatcherQueuePriority.Low, () =>
        {
            StatsTextBlock.Text = $"FPS: {stats.Fps} | Processing: {stats.AvgProcessingMs:F2}ms";
        });
    }
}


