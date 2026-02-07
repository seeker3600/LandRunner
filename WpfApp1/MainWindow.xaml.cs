using System.Windows;
using Vortice.DXGI;

namespace WpfApp1;

/// <summary>
/// MainWindow.xaml の相互作用ロジック
/// </summary>
public partial class MainWindow : Window
{
    private D3D11ImageSource? _imageSource;
    private ScreenCaptureService? _captureService;
    private List<ScreenInfo> _screens = new();

    public MainWindow()
    {
        InitializeComponent();
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        // D3D11ImageSourceとキャプチャサービス初期化
        _imageSource = new D3D11ImageSource();
        _captureService = new ScreenCaptureService(_imageSource, Dispatcher);
        
        // 利用可能なスクリーンを列挙
        LoadScreens();
    }

    private void LoadScreens()
    {
        _screens.Clear();

        try
        {
            using var factory = DXGI.CreateDXGIFactory1<IDXGIFactory1>();
            
            for (uint adapterIndex = 0; ; adapterIndex++)
            {
                var result = factory.EnumAdapters1(adapterIndex, out var adapter);
                if (result.Failure)
                    break;

                using (adapter)
                {
                    for (uint outputIndex = 0; ; outputIndex++)
                    {
                        result = adapter.EnumOutputs(outputIndex, out var output);
                        if (result.Failure)
                            break;

                        using (output)
                        {
                            var output1 = output.QueryInterface<IDXGIOutput1>();
                            var desc = output.Description;

                            _screens.Add(new ScreenInfo
                            {
                                Output = output1,
                                DisplayName = $"{desc.DeviceName} (Adapter {adapterIndex}, Output {outputIndex})",
                                AdapterIndex = unchecked((int)adapterIndex),
                                OutputIndex = unchecked((int)outputIndex)
                            });
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to enumerate screens: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }

        ScreenComboBox.ItemsSource = _screens;
        if (_screens.Count > 0)
        {
            ScreenComboBox.SelectedIndex = 0;
        }
    }

    private void ScreenComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        // スクリーン選択時の処理（必要に応じて実装）
    }

    private void StartButton_Click(object sender, RoutedEventArgs e)
    {
        var selectedScreen = ScreenComboBox.SelectedItem as ScreenInfo;
        if (selectedScreen == null)
        {
            MessageBox.Show("Please select a screen first.", "Warning", 
                MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        if (_imageSource?.ImageSource != null)
        {
            CaptureImage.Source = _imageSource.ImageSource;
        }

        try
        {
            _captureService?.StartCapture(selectedScreen);
            StartButton.IsEnabled = false;
            StopButton.IsEnabled = true;
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Failed to start capture: {ex.Message}", "Error", 
                MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void StopButton_Click(object sender, RoutedEventArgs e)
    {
        _captureService?.StopCapture();
        StartButton.IsEnabled = true;
        StopButton.IsEnabled = false;
    }

    private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
    {
        _captureService?.Dispose();
        _imageSource?.Dispose();
    }
}


