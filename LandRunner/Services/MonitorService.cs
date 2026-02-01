using System.Collections.ObjectModel;
using LandRunner.Native;

namespace LandRunner.Services;

/// <summary>
/// 接続されているモニター情報
/// </summary>
public record MonitorInfo(
    IntPtr Handle,
    string DeviceName,
    System.Windows.Rect Bounds,
    System.Windows.Rect WorkArea,
    bool IsPrimary,
    int Index);

/// <summary>
/// マルチモニター情報を取得するサービス
/// </summary>
public static class MonitorService
{
    /// <summary>
    /// すべてのモニター情報を取得
    /// </summary>
    public static ReadOnlyCollection<MonitorInfo> GetAllMonitors()
    {
        var monitors = new List<MonitorInfo>();
        int index = 0;

        NativeMethods.MonitorEnumProc callback = (hMonitor, hdcMonitor, ref lprcMonitor, dwData) =>
        {
            var info = new NativeMethods.MONITORINFOEX();
            if (NativeMethods.GetMonitorInfo(hMonitor, ref info))
            {
                monitors.Add(new MonitorInfo(
                    hMonitor,
                    info.szDevice.TrimEnd('\0'),
                    new System.Windows.Rect(
                        info.rcMonitor.Left,
                        info.rcMonitor.Top,
                        info.rcMonitor.Width,
                        info.rcMonitor.Height),
                    new System.Windows.Rect(
                        info.rcWork.Left,
                        info.rcWork.Top,
                        info.rcWork.Width,
                        info.rcWork.Height),
                    (info.dwFlags & NativeMethods.MONITORINFOF_PRIMARY) != 0,
                    index++));
            }
            return true;
        };

        NativeMethods.EnumDisplayMonitors(IntPtr.Zero, IntPtr.Zero, callback, IntPtr.Zero);

        return monitors.AsReadOnly();
    }

    /// <summary>
    /// モニター表示名を生成
    /// </summary>
    public static string GetDisplayName(MonitorInfo monitor)
    {
        var primary = monitor.IsPrimary ? " (プライマリ)" : "";
        return $"モニター {monitor.Index + 1}: {monitor.Bounds.Width}x{monitor.Bounds.Height}{primary}";
    }
}
