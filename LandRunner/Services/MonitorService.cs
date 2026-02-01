using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;
using static Windows.Win32.PInvoke;

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

        unsafe
        {
            MONITORENUMPROC callback = (hMonitor, hdcMonitor, lprcMonitor, dwData) =>
            {
                var info = new MONITORINFOEXW();
                info.monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

                if (PInvoke.GetMonitorInfo(hMonitor, ref info.monitorInfo))
                {
                    string deviceName = GetString((char*)&info.szDevice, 32);

                    var friendlyName = friendlyNames.GetValueOrDefault(deviceName, "");

                    var monitorRect = info.monitorInfo.rcMonitor;
                    var workRect = info.monitorInfo.rcWork;

                    var handle = new IntPtr((nint)hMonitor.Value);

                    monitors.Add(new MonitorInfo(
                        handle,
                        deviceName,
                        friendlyName,
                        new System.Windows.Rect(
                            monitorRect.left,
                            monitorRect.top,
                            monitorRect.right - monitorRect.left,
                            monitorRect.bottom - monitorRect.top),
                        new System.Windows.Rect(
                            workRect.left,
                            workRect.top,
                            workRect.right - workRect.left,
                            workRect.bottom - workRect.top),
                        (info.monitorInfo.dwFlags & MONITORINFOF_PRIMARY) != 0,
                        index++));
                }

                return true;
            };

            PInvoke.EnumDisplayMonitors(HDC.Null, null, callback, default);
        }

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
    private static unsafe Dictionary<string, string> GetMonitorFriendlyNames()
    {
        var result = new Dictionary<string, string>();

        try
        {
            // バッファサイズを取得
            uint pathCount, modeCount;
            if (PInvoke.GetDisplayConfigBufferSizes(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, out pathCount, out modeCount) != 0)
                return result;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

            // ディスプレイ構成を取得
            DISPLAYCONFIG_TOPOLOGY_ID topologyId = default;
            if (PInvoke.QueryDisplayConfig(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes, ref topologyId) != 0)
                return result;

            for (var i = 0; i < pathCount; i++)
            {
                var path = paths[i];

                // ソースデバイス名（\\.\DISPLAYx）を取得
                var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                        size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                        adapterId = path.sourceInfo.adapterId,
                        id = path.sourceInfo.id
                    }
                };

                if (PInvoke.DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&sourceName) != 0)
                    continue;

                // ターゲット（モニター）のフレンドリー名を取得
                var targetName = new DISPLAYCONFIG_TARGET_DEVICE_NAME
                {
                    header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
                    {
                        type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                        size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                        adapterId = path.targetInfo.adapterId,
                        id = path.targetInfo.id
                    }
                };

                if (PInvoke.DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&targetName) != 0)
                    continue;

                string gdiDeviceName = GetString((char*)&sourceName.viewGdiDeviceName, 32);
                string friendlyName = GetString((char*)&targetName.monitorFriendlyDeviceName, 64);

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

    private static string GetString(ReadOnlySpan<char> buffer)
    {
        var span = buffer;
        var end = span.IndexOf('\0');
        if (end >= 0)
        {
            span = span[..end];
        }
        return new string(span);
    }

    private static unsafe string GetString(char* buffer, int length)
    {
        var span = new ReadOnlySpan<char>(buffer, length);
        return GetString(span);
    }
}
