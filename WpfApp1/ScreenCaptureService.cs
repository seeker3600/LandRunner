using System.Windows;
using System.Windows.Threading;
using Vortice.Direct3D;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WpfApp1;

/// <summary>
/// スクリーンキャプチャサービス
/// Desktop Duplication APIを使用して低レイテンシでキャプチャ
/// </summary>
public class ScreenCaptureService : IDisposable
{
    private readonly D3D11ImageSource _imageSource;
    private readonly Dispatcher _dispatcher;
    
    private ID3D11Device? _d3dDevice;
    private ID3D11DeviceContext? _deviceContext;
    private IDXGIOutputDuplication? _duplication;
    private ID3D11Texture2D? _stagingTexture;
    private CancellationTokenSource? _cts;
    private Task? _captureTask;
    private bool _disposed;

    public ScreenCaptureService(D3D11ImageSource imageSource, Dispatcher dispatcher)
    {
        _imageSource = imageSource;
        _dispatcher = dispatcher;
    }

    public void StartCapture(ScreenInfo screenInfo)
    {
        StopCapture();

        // Direct3D11デバイス作成
        var result = D3D11.D3D11CreateDevice(
            null,
            DriverType.Hardware,
            DeviceCreationFlags.BgraSupport,
            new[] { FeatureLevel.Level_11_0 },
            out _d3dDevice);

        if (result.Failure || _d3dDevice == null)
        {
            throw new Exception($"Failed to create D3D11 device: {result}");
        }

        _deviceContext = _d3dDevice.ImmediateContext;
        _imageSource.SetDevice(_d3dDevice);

        // Output Duplication作成
        _duplication = screenInfo.Output.DuplicateOutput(_d3dDevice);

        // キャプチャタスク開始
        _cts = new CancellationTokenSource();
        _captureTask = Task.Run(() => CaptureLoop(_cts.Token), _cts.Token);
    }

    public void StopCapture()
    {
        _cts?.Cancel();
        _captureTask?.Wait(TimeSpan.FromSeconds(1));
        
        _stagingTexture?.Dispose();
        _stagingTexture = null;

        _duplication?.Dispose();
        _duplication = null;

        _deviceContext?.Dispose();
        _deviceContext = null;

        _d3dDevice?.Dispose();
        _d3dDevice = null;

        _cts?.Dispose();
        _cts = null;
    }

    private void CaptureLoop(CancellationToken token)
    {
        while (!token.IsCancellationRequested)
        {
            try
            {
                if (_duplication == null || _d3dDevice == null || _deviceContext == null)
                    break;

                // フレーム取得
                var result = _duplication.AcquireNextFrame(1000, out var frameInfo, out var desktopResource);
                
                if (result.Failure)
                {
                    if (result == Vortice.DXGI.ResultCode.WaitTimeout)
                    {
                        Thread.Sleep(1);
                        continue;
                    }
                    break;
                }

                using (desktopResource)
                {
                    if (frameInfo.AccumulatedFrames == 0)
                    {
                        _duplication.ReleaseFrame();
                        continue;
                    }

                    using var desktopTexture = desktopResource.QueryInterface<ID3D11Texture2D>();
                    var desc = desktopTexture.Description;

                    // ステージングテクスチャ作成（初回のみ）
                    if (_stagingTexture == null)
                    {
                        var stagingDesc = new Texture2DDescription
                        {
                            Width = desc.Width,
                            Height = desc.Height,
                            MipLevels = 1,
                            ArraySize = 1,
                            Format = desc.Format,
                            SampleDescription = new SampleDescription(1, 0),
                            Usage = ResourceUsage.Default,
                            BindFlags = BindFlags.ShaderResource,
                            CPUAccessFlags = CpuAccessFlags.None,
                            MiscFlags = ResourceOptionFlags.None
                        };

                        _stagingTexture = _d3dDevice.CreateTexture2D(stagingDesc);
                    }

                    // テクスチャコピー
                    _deviceContext.CopyResource(_stagingTexture, desktopTexture);

                    // WPFのUIスレッドで画像更新
                    _dispatcher.Invoke(() =>
                    {
                        if (!_disposed && _stagingTexture != null)
                        {
                            _imageSource.UpdateFromTexture(_stagingTexture);
                        }
                    }, DispatcherPriority.Render);

                    _duplication.ReleaseFrame();
                }

                // フレームレート制限（約60fps）
                Thread.Sleep(16);
            }
            catch (Exception)
            {
                // エラー時は再試行
                Thread.Sleep(100);
            }
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        StopCapture();
        _disposed = true;
        GC.SuppressFinalize(this);
    }
}


