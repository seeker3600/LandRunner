using System.Runtime.InteropServices;

namespace LandRunner.Native;

/// <summary>
/// Win32 API 呼び出しのためのネイティブメソッド
/// </summary>
internal static partial class NativeMethods
{
    public const int WDA_EXCLUDEFROMCAPTURE = 0x00000011;

    /// <summary>
    /// ウィンドウの表示アフィニティを設定
    /// WDA_EXCLUDEFROMCAPTURE を設定すると、ウィンドウがスクリーンキャプチャから除外される
    /// </summary>
    [DllImport("user32.dll", SetLastError = true)]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool SetWindowDisplayAffinity(IntPtr hWnd, uint dwAffinity);

    /// <summary>
    /// モニター列挙用コールバック
    /// </summary>
    public delegate bool MonitorEnumProc(IntPtr hMonitor, IntPtr hdcMonitor, ref RECT lprcMonitor, IntPtr dwData);

    /// <summary>
    /// すべてのモニターを列挙
    /// </summary>
    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    public static extern bool EnumDisplayMonitors(IntPtr hdc, IntPtr lprcClip, MonitorEnumProc lpfnEnum, IntPtr dwData);

    /// <summary>
    /// モニター情報を取得
    /// </summary>
    [DllImport("user32.dll", CharSet = CharSet.Auto)]
    public static extern bool GetMonitorInfo(IntPtr hMonitor, ref MONITORINFOEX lpmi);

    [StructLayout(LayoutKind.Sequential)]
    public struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;

        public int Width => Right - Left;
        public int Height => Bottom - Top;
    }

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Auto)]
    public struct MONITORINFOEX
    {
        public int cbSize;
        public RECT rcMonitor;
        public RECT rcWork;
        public uint dwFlags;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 32)]
        public string szDevice;

        public MONITORINFOEX()
        {
            cbSize = Marshal.SizeOf<MONITORINFOEX>();
            rcMonitor = default;
            rcWork = default;
            dwFlags = 0;
            szDevice = string.Empty;
        }
    }



    public const uint MONITORINFOF_PRIMARY = 1;
}
