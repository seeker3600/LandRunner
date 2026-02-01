using System.Windows;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using GlassBridge;
using LandRunner.Native;
using LandRunner.Services;
using Microsoft.Extensions.Logging;

namespace LandRunner.Views;

/// <summary>
/// キャプチャ画像を表示し、XRデバイスの姿勢に応じて位置を変化させるウィンドウ
/// </summary>
public partial class DisplayWindow : Window, IAsyncDisposable
{
    private readonly ILogger<DisplayWindow> _logger = App.CreateLogger<DisplayWindow>();

    private readonly IImuDevice _device;
    private readonly MonitorInfo _displayMonitor;
    private readonly MonitorInfo _captureMonitor;

    private readonly DesktopCaptureService _captureService;
    private readonly HeadTrackingService _trackingService;

    private bool _isDebugVisible;
    private bool _disposed;

    // 画面の移動量スケール（ピクセル単位）
    // 正規化オフセット（-1～1）をピクセルに変換する係数
    private double _horizontalScale;
    private double _verticalScale;

    public DisplayWindow(
        IImuDevice device,
        MonitorInfo displayMonitor,
        MonitorInfo captureMonitor)
    {
        InitializeComponent();

        _device = device;
        _displayMonitor = displayMonitor;
        _captureMonitor = captureMonitor;

        _captureService = new DesktopCaptureService();
        _trackingService = new HeadTrackingService();

        Loaded += OnLoaded;
        Closed += OnClosed;
    }

    private async void OnLoaded(object sender, RoutedEventArgs e)
    {
        // ウィンドウを表示用モニターに全画面配置
        PositionOnMonitor();

        // WDA_EXCLUDEFROMCAPTURE を設定（このウィンドウをキャプチャから除外）
        SetExcludeFromCapture();

        // スケール計算
        _horizontalScale = _displayMonitor.Bounds.Width * 0.3;  // 視野端で30%移動
        _verticalScale = _displayMonitor.Bounds.Height * 0.3;

        // イベントハンドラ登録
        _captureService.FrameCaptured += OnFrameCaptured;
        _trackingService.TrackingUpdated += OnTrackingUpdated;

        // キャプチャ開始
        try
        {
            await _captureService.StartCaptureAsync(_captureMonitor);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "キャプチャの開始に失敗しました");
            MessageBox.Show($"キャプチャの開始に失敗しました:\n{ex.Message}", "エラー",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Close();
            return;
        }

        // トラッキング開始
        _trackingService.StartTracking(_device);

        _logger.LogInformation("DisplayWindow 初期化完了 (表示: {DisplayMonitor}, キャプチャ: {CaptureMonitor})",
            _displayMonitor.DeviceName, _captureMonitor.DeviceName);
    }

    private void OnClosed(object? sender, EventArgs e)
    {
        _ = DisposeAsync();
    }

    /// <summary>
    /// ウィンドウを表示用モニターに配置
    /// </summary>
    private void PositionOnMonitor()
    {
        var bounds = _displayMonitor.Bounds;

        Left = bounds.Left;
        Top = bounds.Top;
        Width = bounds.Width;
        Height = bounds.Height;

        WindowState = WindowState.Normal;
    }

    /// <summary>
    /// WDA_EXCLUDEFROMCAPTURE を設定
    /// </summary>
    private void SetExcludeFromCapture()
    {
        var hwnd = new WindowInteropHelper(this).Handle;
        if (hwnd != IntPtr.Zero)
        {
            var result = NativeMethods.SetWindowDisplayAffinity(hwnd, NativeMethods.WDA_EXCLUDEFROMCAPTURE);
            if (!result)
            {
                System.Diagnostics.Debug.WriteLine("?? SetWindowDisplayAffinity failed");
            }
        }
    }

    /// <summary>
    /// キャプチャフレーム受信時
    /// </summary>
    private void OnFrameCaptured(object? sender, WriteableBitmap bitmap)
    {
        // UI スレッドで画像を更新
        Dispatcher.BeginInvoke(() =>
        {
            CaptureImage.Source = bitmap;
        });
    }

    /// <summary>
    /// トラッキングデータ更新時
    /// </summary>
    private void OnTrackingUpdated(object? sender, TrackingData data)
    {
        Dispatcher.BeginInvoke(() =>
        {
            // 頭の動きと反対方向に画像を移動（空間固定効果）
            // 頭を右に向けると、画像は左に移動するように見える = 画像を右に動かす
            ImageTranslation.X = data.HorizontalOffset * _horizontalScale;
            ImageTranslation.Y = data.VerticalOffset * _verticalScale;

            // 頭のロールと反対方向に画像を回転
            ImageRotation.Angle = data.RotationAngle;

            // デバッグ情報更新
            if (_isDebugVisible)
            {
                var euler = data.RawAngles;
                DebugText.Text = $"""
                    Roll:  {euler.Roll,7:F1}°
                    Pitch: {euler.Pitch,7:F1}°
                    Yaw:   {euler.Yaw,7:F1}°
                    ───────────────
                    H-Offset: {data.HorizontalOffset,6:F2}
                    V-Offset: {data.VerticalOffset,6:F2}
                    Rotation: {data.RotationAngle,6:F1}°
                    ───────────────
                    X: {ImageTranslation.X,6:F0} px
                    Y: {ImageTranslation.Y,6:F0} px
                    """;
            }
        });
    }

    /// <summary>
    /// キー入力処理
    /// </summary>
    private void Window_KeyDown(object sender, KeyEventArgs e)
    {
        switch (e.Key)
        {
            case Key.Escape:
                Close();
                break;

            case Key.R:
                // 姿勢リセット
                _trackingService.ResetReference();
                break;

            case Key.D:
                // デバッグ表示トグル
                _isDebugVisible = !_isDebugVisible;
                DebugOverlay.Visibility = _isDebugVisible ? Visibility.Visible : Visibility.Collapsed;
                break;
        }
    }

    public async ValueTask DisposeAsync()
    {
        if (_disposed) return;
        _disposed = true;

        _logger.LogInformation("DisplayWindow を閉じます");

        _captureService.FrameCaptured -= OnFrameCaptured;
        _trackingService.TrackingUpdated -= OnTrackingUpdated;

        await _trackingService.StopTrackingAsync();
        _trackingService.Dispose();

        _captureService.Dispose();

        await _device.DisposeAsync();
    }
}
