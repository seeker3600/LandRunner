using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Capture;
using Windows.Graphics.DirectX;
using Windows.Graphics.DirectX.Direct3D11;
using System.Windows.Media.Imaging;
using System.Windows.Media;
using System.Windows;
using Vortice.Direct3D11;
using Vortice.DXGI;
using WinRT;
using Microsoft.Extensions.Logging;

namespace LandRunner.Services;

/// <summary>
/// Windows.Graphics.Capture を使用したデスクトップキャプチャサービス
/// </summary>
public sealed class DesktopCaptureService : IDisposable
{
    private readonly ILogger<DesktopCaptureService> _logger = App.CreateLogger<DesktopCaptureService>();

    private Direct3D11CaptureFramePool? _framePool;
    private GraphicsCaptureSession? _session;
    private IDirect3DDevice? _winrtDevice;
    private ID3D11Device? _d3dDevice;
    private ID3D11DeviceContext? _d3dContext;
    private SizeInt32 _lastSize;
    private bool _disposed;

    /// <summary>
    /// 新しいフレームがキャプチャされたときに発生
    /// </summary>
    public event EventHandler<WriteableBitmap>? FrameCaptured;

    /// <summary>
    /// キャプチャ中のフレームサイズ
    /// </summary>
    public SizeInt32 FrameSize => _lastSize;

    /// <summary>
    /// 指定されたモニターのキャプチャを開始
    /// </summary>
    public async Task StartCaptureAsync(MonitorInfo monitor)
    {
        if (_disposed) throw new ObjectDisposedException(nameof(DesktopCaptureService));

        _logger.LogInformation("キャプチャ開始: {MonitorName} ({Width}x{Height})",
            monitor.DeviceName, monitor.Bounds.Width, monitor.Bounds.Height);

        // GraphicsCaptureItem を取得
        var item = GetCaptureItemForMonitor(monitor);
        if (item == null)
        {
            _logger.LogError("モニター '{MonitorName}' のキャプチャアイテム取得に失敗", monitor.DeviceName);
            throw new InvalidOperationException($"モニター '{monitor.DeviceName}' のキャプチャアイテムを取得できませんでした");
        }

        // Direct3D デバイスを作成
        CreateD3DDevice();
        _lastSize = item.Size;

        // フレームプールを作成
        _framePool = Direct3D11CaptureFramePool.CreateFreeThreaded(
            _winrtDevice!,
            DirectXPixelFormat.B8G8R8A8UIntNormalized,
            2,
            _lastSize);

        _framePool.FrameArrived += OnFrameArrived;

        // キャプチャセッションを開始
        _session = _framePool.CreateCaptureSession(item);
        _session.IsBorderRequired = false;
        _session.IsCursorCaptureEnabled = true;
        _session.StartCapture();

        await Task.CompletedTask;
    }

    /// <summary>
    /// キャプチャを停止
    /// </summary>
    public void StopCapture()
    {
        _logger.LogInformation("キャプチャ停止");

        _session?.Dispose();
        _session = null;

        _framePool?.Dispose();
        _framePool = null;

        _d3dContext?.Dispose();
        _d3dContext = null;

        _d3dDevice?.Dispose();
        _d3dDevice = null;

        _winrtDevice?.Dispose();
        _winrtDevice = null;
    }

    private void OnFrameArrived(Direct3D11CaptureFramePool sender, object args)
    {
        using var frame = sender.TryGetNextFrame();
        if (frame == null) return;

        // サイズが変わった場合はフレームプールを再作成
        if (frame.ContentSize.Width != _lastSize.Width || frame.ContentSize.Height != _lastSize.Height)
        {
            _lastSize = frame.ContentSize;
            _framePool?.Recreate(_winrtDevice!, DirectXPixelFormat.B8G8R8A8UIntNormalized, 2, _lastSize);
        }

        // フレームを WriteableBitmap に変換
        var bitmap = ConvertFrameToBitmap(frame);
        if (bitmap != null)
        {
            FrameCaptured?.Invoke(this, bitmap);
        }
    }

    private unsafe WriteableBitmap? ConvertFrameToBitmap(Direct3D11CaptureFrame frame)
    {
        var surface = frame.Surface;
        if (surface == null || _d3dDevice == null || _d3dContext == null) return null;

        try
        {
            // WinRT サーフェスから D3D テクスチャを取得
            var access = surface.As<IDirect3DDxgiInterfaceAccess>();
            var texturePtr = access.GetInterface(typeof(ID3D11Texture2D).GUID);
            using var sourceTexture = new ID3D11Texture2D(texturePtr);

            var desc = sourceTexture.Description;
            var width = desc.Width;
            var height = desc.Height;

            // ステージングテクスチャを作成
            var stagingDesc = new Texture2DDescription
            {
                Width = width,
                Height = height,
                MipLevels = 1,
                ArraySize = 1,
                Format = Format.B8G8R8A8_UNorm,
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Staging,
                BindFlags = BindFlags.None,
                CPUAccessFlags = CpuAccessFlags.Read,
                MiscFlags = ResourceOptionFlags.None
            };

            using var stagingTexture = _d3dDevice.CreateTexture2D(stagingDesc);
            _d3dContext.CopyResource(stagingTexture, sourceTexture);

            // データを読み取り
            var mappedResource = _d3dContext.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);

            // WriteableBitmap must be created on UI thread
            WriteableBitmap? bitmap = null;
            Application.Current.Dispatcher.Invoke(() =>
            {
                bitmap = new WriteableBitmap((int)width, (int)height, 96, 96, PixelFormats.Bgra32, null);
                bitmap.Lock();

                for (int y = 0; y < height; y++)
                {
                    var sourcePtr = IntPtr.Add(mappedResource.DataPointer, (int)(y * mappedResource.RowPitch));
                    var destPtr = IntPtr.Add(bitmap.BackBuffer, y * bitmap.BackBufferStride);
                    Buffer.MemoryCopy(sourcePtr.ToPointer(), destPtr.ToPointer(), width * 4, width * 4);
                }

                bitmap.AddDirtyRect(new Int32Rect(0, 0, (int)width, (int)height));
                bitmap.Unlock();
                bitmap.Freeze();
            });

            _d3dContext.Unmap(stagingTexture, 0);

            return bitmap;
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "フレームのビットマップ変換に失敗しました");
            return null;
        }
    }

    private static GraphicsCaptureItem? GetCaptureItemForMonitor(MonitorInfo monitor)
    {
        var interop = GraphicsCaptureItem.As<IGraphicsCaptureItemInterop>();
        var itemPointer = interop.CreateForMonitor(monitor.Handle);
        if (itemPointer == IntPtr.Zero) return null;

        var item = GraphicsCaptureItem.FromAbi(itemPointer);
        Marshal.Release(itemPointer);
        return item;
    }

    private void CreateD3DDevice()
    {
        // D3D11 デバイスを作成
        Vortice.Direct3D11.D3D11.D3D11CreateDevice(
            null,
            Vortice.Direct3D.DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            [],
            out _d3dDevice,
            out _d3dContext);

        // DXGI デバイスを取得
        using var dxgiDevice = _d3dDevice!.QueryInterface<IDXGIDevice>();

        // WinRT IDirect3DDevice に変換
        var inspectable = CreateDirect3D11DeviceFromDXGIDevice(dxgiDevice.NativePointer);
        _winrtDevice = MarshalInterface<IDirect3DDevice>.FromAbi(inspectable);
    }

    [DllImport("d3d11.dll", EntryPoint = "CreateDirect3D11DeviceFromDXGIDevice", SetLastError = true)]
    private static extern IntPtr CreateDirect3D11DeviceFromDXGIDevice(IntPtr dxgiDevice);

    [ComImport]
    [Guid("A9B3D012-3DF2-4EE3-B8D1-8695F457D3C1")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IDirect3DDxgiInterfaceAccess
    {
        IntPtr GetInterface([In] ref Guid iid);
    }

    [ComImport]
    [Guid("3628E81B-3CAC-4C60-B7F4-23CE0E0C3356")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IGraphicsCaptureItemInterop
    {
        IntPtr CreateForWindow(IntPtr window);
        IntPtr CreateForMonitor(IntPtr monitor);
    }

    public void Dispose()
    {
        if (_disposed) return;
        _disposed = true;
        StopCapture();
    }
}

