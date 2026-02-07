using Microsoft.Graphics.Canvas;
using Microsoft.Graphics.Canvas.UI.Composition;
using Microsoft.UI.Composition;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Threading;
using System.Threading.Tasks;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.UI;
using Windows.Win32;
using Windows.Win32.Graphics.Gdi;
using Windows.Win32.System.WinRT;
using Windows.Win32.System.WinRT.Graphics.Capture;
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

    public event EventHandler<SpriteVisual>? VisualCreated;

    public async Task<bool> StartCaptureAsync(DisplayMonitorInfo displayInfo, Compositor compositor)
    {
        try
        {
            Stop();

            _canvasDevice = new CanvasDevice();
            _compositionGraphicsDevice = CanvasComposition.CreateCompositionGraphicsDevice(compositor, _canvasDevice);

            var item = CreateCaptureItemForDisplay((HMONITOR)displayInfo.Handle);
            if (item == null)
                return false;

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

    private static WindowsDeleteStringSafeHandle ToHS(string s)
    {
        int hr = PInvoke.WindowsCreateString(s, (uint)s.Length, out var hs);
        Marshal.ThrowExceptionForHR(hr);
        return hs;
    }

    private static T GetActivationFactory<T>(string runtimeClassId) where T : class
    {
        using var cls = ToHS(runtimeClassId);
        Guid iid = typeof(T).GUID;

        var hr = PInvoke.RoGetActivationFactory(cls, in iid, out object factory);
        Marshal.ThrowExceptionForHR(hr.Value);

        return (T)factory;
    }

    private static GraphicsCaptureItem? CreateCaptureItemForDisplay(in HMONITOR hMonitor)
    {
        try
        {
            //var factory = GetActivationFactory<IGraphicsCaptureItemInterop>("Windows.Graphics.Capture.GraphicsCaptureItem");
            //using (ComReleaser.AsDisposable(factory))
            //{
            //    //Guid itemGuid = typeof(Windows.Graphics.Capture.IGraphicsCaptureItem).GUID;
            //    var test = Guid.Parse("79C3F95B-31F7-4EC2-A464-632EF5D30760"); // IGraphicsCaptureItemInterop
            //    factory.CreateForMonitor(hMonitor, in test, out var item);
            var displayId = new DisplayId((ulong)(IntPtr)hMonitor);
            var res = GraphicsCaptureItem.TryCreateFromDisplayId(displayId);
            return res;
                //return (GraphicsCaptureItem)item;
            //}
        }
        catch (Exception ex)
        {
            Debug.WriteLine($"Error creating capture item: {ex.Message}");
        }
        return null;
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        if (!_isCapturing || _canvasDevice == null || _surface == null)
            return;

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





