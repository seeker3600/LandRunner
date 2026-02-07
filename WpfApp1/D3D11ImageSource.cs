using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Vortice.Direct3D11;
using Vortice.DXGI;

namespace WpfApp1;

/// <summary>
/// Direct3D11テクスチャをWPFのWriteableBitmapとして表示するためのクラス
/// （D3DImageはD3D9 Interopが必要なため、シンプルにWriteableBitmapを使用）
/// </summary>
public class D3D11ImageSource : IDisposable
{
    private ID3D11Device? _device;
    private ID3D11DeviceContext? _context;
    private WriteableBitmap? _bitmap;
    private bool _disposed;

    public ImageSource? ImageSource => _bitmap;

    public void SetDevice(ID3D11Device device)
    {
        _device = device;
        _context = device.ImmediateContext;
    }

    public void UpdateFromTexture(ID3D11Texture2D texture)
    {
        if (_device == null || _context == null || texture == null)
            return;

        var desc = texture.Description;

        // WriteableBitmap作成（初回のみ）
        if (_bitmap == null || _bitmap.PixelWidth != unchecked((int)desc.Width) || _bitmap.PixelHeight != unchecked((int)desc.Height))
        {
            _bitmap = new WriteableBitmap(
                unchecked((int)desc.Width),
                unchecked((int)desc.Height),
                96, 96,
                PixelFormats.Bgra32,
                null);
        }

        // ステージングテクスチャを作成してCPUで読み取り可能にする
        var stagingDesc = new Texture2DDescription
        {
            Width = desc.Width,
            Height = desc.Height,
            MipLevels = 1,
            ArraySize = 1,
            Format = desc.Format,
            SampleDescription = new SampleDescription(1, 0),
            Usage = ResourceUsage.Staging,
            BindFlags = BindFlags.None,
            CPUAccessFlags = CpuAccessFlags.Read,
            MiscFlags = ResourceOptionFlags.None
        };

        using var stagingTexture = _device.CreateTexture2D(stagingDesc);
        _context.CopyResource(stagingTexture, texture);

        // テクスチャデータを読み取り
        var mappedResource = _context.Map(stagingTexture, 0, MapMode.Read, Vortice.Direct3D11.MapFlags.None);
        
        try
        {
            _bitmap.Lock();
            
            unsafe
            {
                var source = (byte*)mappedResource.DataPointer;
                var dest = (byte*)_bitmap.BackBuffer;
                var stride = _bitmap.BackBufferStride;
                var rowPitch = mappedResource.RowPitch;

                for (int y = 0; y < desc.Height; y++)
                {
                    Buffer.MemoryCopy(
                        source + y * rowPitch,
                        dest + y * stride,
                        stride,
                        Math.Min(stride, rowPitch));
                }
            }

            _bitmap.AddDirtyRect(new Int32Rect(0, 0, unchecked((int)desc.Width), unchecked((int)desc.Height)));
        }
        finally
        {
            _bitmap.Unlock();
            _context.Unmap(stagingTexture, 0);
        }
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        _bitmap = null;
        _context = null;
        _device = null;

        _disposed = true;
        GC.SuppressFinalize(this);
    }
}

