using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;

namespace WinUiApp1;

sealed partial class ComReleaser : IDisposable
{
    public static IDisposable AsDisposable(object com)
        => new ComReleaser(com);

    private object? _com;
    public ComReleaser(object com) => _com = com;
    public void Dispose()
    {
        if (_com is not null)
        {
            Marshal.ReleaseComObject(_com);
            _com = null;
        }
    }
}
