using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Composition;
using System.Diagnostics;
using System.Numerics;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.UI;
using Win2DDirectX = Microsoft.Graphics.DirectX;
using WinDirectX = Windows.Graphics.DirectX;

namespace WinUiApp1;

public sealed class ScreenCaptureService : IDisposable
{
    private GraphicsCaptureItem? _captureItem;
    private Direct3D11CaptureFramePool? _framePool;
    private GraphicsCaptureSession? _session;
    private CanvasDevice? _canvasDevice;
    private CompositionGraphicsDevice? _compositionGraphicsDevice;
    private CompositionDrawingSurface? _surface;
    private SpriteVisual? _visual;
    private SizeInt32 _lastSize;
    private bool _isCapturing;

    // Latency measurement fields
    private readonly Stopwatch _frameStopwatch = new();
    private int _frameCount;
    private double _totalProcessingMs;
    private DateTime _lastStatsTime = DateTime.UtcNow;

    public event EventHandler<SpriteVisual>? VisualCreated;
    public event EventHandler<CaptureStats>? StatsUpdated;

    public async Task<bool> StartCaptureAsync(DisplayMonitorInfo displayInfo, Compositor compositor)
    {
        try
        {
            Stop();

            _canvasDevice = new CanvasDevice();
            _compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, _canvasDevice);

            var item = GraphicsCaptureItem.TryCreateFromDisplayId(displayInfo.Id);

            _captureItem = item;
            _lastSize = _captureItem.Size;

            _framePool = Direct3D11CaptureFramePool.Create(
                _canvasDevice,
                WinDirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                2,
                _captureItem.Size);

            _surface = _compositionGraphicsDevice.CreateDrawingSurface(
                new Windows.Foundation.Size(400, 400),
                Win2DDirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized,
                Win2DDirectX.DirectXAlphaMode.Premultiplied);

            _visual = compositor.CreateSpriteVisual();
            _visual.RelativeSizeAdjustment = Vector2.One;
            var brush = compositor.CreateSurfaceBrush(_surface);
            brush.HorizontalAlignmentRatio = 0.5f;
            brush.VerticalAlignmentRatio = 0.5f;
            brush.Stretch = CompositionStretch.Uniform;
            _visual.Brush = brush;

            _framePool.FrameArrived += OnFrameArrived;

            _session = _framePool.CreateCaptureSession(_captureItem);
            _session.IsBorderRequired = false;
            _session.IsCursorCaptureEnabled = true;
            _session.StartCapture();
            _isCapturing = true;

            VisualCreated?.Invoke(this, _visual);

            await Task.CompletedTask;
            return true;
        }
        catch
        {
            Stop();
            return false;
        }
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        if (!_isCapturing || _canvasDevice == null || _surface == null)
            return;

        _frameStopwatch.Restart();

        try
        {
            using var frame = sender.TryGetNextFrame();
            if (frame == null)
                return;

            var contentSize = frame.ContentSize;
            if (contentSize.Width != _lastSize.Width || contentSize.Height != _lastSize.Height)
            {
                _lastSize = contentSize;
                sender.Recreate(_canvasDevice, WinDirectX.DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, contentSize);
                return;
            }

            using var canvasBitmap = CanvasBitmap.CreateFromDirect3D11Surface(_canvasDevice, frame.Surface);
            CanvasComposition.Resize(_surface, canvasBitmap.Size);

            using var session = CanvasComposition.CreateDrawingSession(_surface);
            session.Clear(Color.FromArgb(0, 0, 0, 0));
            session.DrawImage(canvasBitmap);

            _frameStopwatch.Stop();
            _totalProcessingMs += _frameStopwatch.Elapsed.TotalMilliseconds;
            _frameCount++;

            // Emit stats every second
            var now = DateTime.UtcNow;
            if ((now - _lastStatsTime).TotalSeconds >= 1.0 && _frameCount > 0)
            {
                var stats = new CaptureStats(
                    Fps: _frameCount,
                    AvgProcessingMs: _totalProcessingMs / _frameCount
                );

                Debug.WriteLine($"FPS: {stats.Fps:F0}, Processing: {stats.AvgProcessingMs:F2}ms");
                StatsUpdated?.Invoke(this, stats);

                // Reset counters
                _frameCount = 0;
                _totalProcessingMs = 0;
                _lastStatsTime = now;
            }
        }
        catch
        {
        }
    }

    public void Stop()
    {
        _isCapturing = false;
        _session?.Dispose();
        _session = null;
        _framePool?.Dispose();
        _framePool = null;
        _captureItem = null;
    }

    public void Dispose()
    {
        Stop();
        _surface?.Dispose();
        _compositionGraphicsDevice?.Dispose();
        _canvasDevice?.Dispose();
    }
}

public record CaptureStats(
    int Fps,
    double AvgProcessingMs
);
