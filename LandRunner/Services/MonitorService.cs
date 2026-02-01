using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using LandRunner.Native;

namespace LandRunner.Services;

/// <summary>
/// 接続されているモニター情報
/// </summary>
public record MonitorInfo(
    IntPtr Handle,
    string DeviceName,
    string FriendlyName,
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
        // まずフレンドリー名のマッピングを取得
        var friendlyNames = GetMonitorFriendlyNames();

        var monitors = new List<MonitorInfo>();
        int index = 0;

        NativeMethods.MonitorEnumProc callback = (hMonitor, hdcMonitor, ref lprcMonitor, dwData) =>
        {
            var info = new NativeMethods.MONITORINFOEX();
            if (NativeMethods.GetMonitorInfo(hMonitor, ref info))
            {
                var deviceName = info.szDevice.TrimEnd('\0');
                var friendlyName = friendlyNames.GetValueOrDefault(deviceName, "");

                monitors.Add(new MonitorInfo(
                    hMonitor,
                    deviceName,
                    friendlyName,
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
        var name = !string.IsNullOrEmpty(monitor.FriendlyName) ? monitor.FriendlyName : monitor.DeviceName;
        return $"モニター {monitor.Index + 1}: {name} ({monitor.Bounds.Width}x{monitor.Bounds.Height}){primary}";
    }

    /// <summary>
    /// DisplayConfig API を使用してモニターのフレンドリー名を取得
    /// </summary>
    private static Dictionary<string, string> GetMonitorFriendlyNames()
    {
        var result = new Dictionary<string, string>();

        try
        {
            // バッファサイズを取得
            int pathCount, modeCount;
            if (NativeMethods.GetDisplayConfigBufferSizes(NativeMethods.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount) != 0)
                return result;

            var paths = new NativeMethods.DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new NativeMethods.DISPLAYCONFIG_MODE_INFO[modeCount];

            // ディスプレイ構成を取得
            if (NativeMethods.QueryDisplayConfig(NativeMethods.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, IntPtr.Zero) != 0)
                return result;

            foreach (var path in paths)
            {
                // ソースデバイス名（\\.\DISPLAYx）を取得
                var sourceName = new NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header = new NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size = (uint)Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                        adapterId = path.sourceInfo.adapterId,
                        id = path.sourceInfo.id
                    }
                };

                if (NativeMethods.DisplayConfigGetDeviceInfo(ref sourceName) != 0)
                    continue;

                // ターゲット（モニター）のフレンドリー名を取得
                var targetName = new NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME
                {
                    header = new NativeMethods.DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = NativeMethods.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                        size = (uint)Marshal.SizeOf<NativeMethods.DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                        adapterId = path.targetInfo.adapterId,
                        id = path.targetInfo.id
                    }
                };

                if (NativeMethods.DisplayConfigGetDeviceInfo(ref targetName) != 0)
                    continue;

                var gdiDeviceName = sourceName.viewGdiDeviceName.TrimEnd('\0');
                var friendlyName = targetName.monitorFriendlyDeviceName.TrimEnd('\0');

                if (!string.IsNullOrEmpty(gdiDeviceName) && !string.IsNullOrEmpty(friendlyName))
                {
                    result[gdiDeviceName] = friendlyName;
                }
            }
        }
        catch
        {
            // API失敗時は空の辞書を返す
        }

        return result;
    }
}
