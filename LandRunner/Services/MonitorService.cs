using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using System.Windows;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace LandRunner.Services;

/// <summary>
/// 接続されているモニター情報
/// </summary>
public record MonitorInfo(
    IntPtr Handle,
    string DeviceName,
    string FriendlyName,
    Rect Bounds,
    Rect WorkArea,
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
        // 1. DisplayConfig API で高精度のFriendlyNameを取得（キャッシュ）
        var friendlyNamesCache = GetMonitorFriendlyNamesCache();
        
        var monitors = new List<MonitorInfo>();
        int index = 0;

        unsafe
        {
            PInvoke.EnumDisplayMonitors(HDC.Null, null, (hMonitor, _, _, _) =>
            {
                var info = new MONITORINFOEXW();
                info.monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

                if (PInvoke.GetMonitorInfo(hMonitor, ref info.monitorInfo))
                {
                    // デバイス名取得 (NULL終端を考慮して文字列化)
                    var deviceName = GetString(info.szDevice.AsSpan());

                    // FriendlyName を取得 (Cache -> Fallback)
                    string friendlyName = GetFriendlyName(deviceName, friendlyNamesCache);

                    var monitorRect = info.monitorInfo.rcMonitor;
                    var workRect = info.monitorInfo.rcWork;

                    monitors.Add(new MonitorInfo(
                        (IntPtr)hMonitor,
                        deviceName,
                        friendlyName,
                        new Rect(monitorRect.left, monitorRect.top, monitorRect.right - monitorRect.left, monitorRect.bottom - monitorRect.top),
                        new Rect(workRect.left, workRect.top, workRect.right - workRect.left, workRect.bottom - workRect.top),
                        (info.monitorInfo.dwFlags & PInvoke.MONITORINFOF_PRIMARY) != 0,
                        index++));
                }

                return true;
            }, 0);
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
    /// デバイス名に対応する FriendlyName を取得
    /// </summary>
    private static string GetFriendlyName(string deviceName, Dictionary<string, string> cache)
    {
        // 1. QueryDisplayConfig のキャッシュから検索 (e.g., "LG UltraFine")
        if (cache.TryGetValue(deviceName, out var name) && !string.IsNullOrEmpty(name))
        {
            return name;
        }

        // 2. EnumDisplayDevices で取得トライ (e.g., "Generic PnP Monitor")
        return GetFriendlyNameViaEnum(deviceName);
    }

    /// <summary>
    /// DisplayConfig API を使用してモニターのフレンドリー名を取得（高精度）
    /// </summary>
    private static Dictionary<string, string> GetMonitorFriendlyNamesCache()
    {
        var result = new Dictionary<string, string>();

        try
        {
            if (PInvoke.GetDisplayConfigBufferSizes(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, out var pathCount, out var modeCount) != 0)
                return result;

            var paths = new DISPLAYCONFIG_PATH_INFO[pathCount];
            var modes = new DISPLAYCONFIG_MODE_INFO[modeCount];

            if (PInvoke.QueryDisplayConfig(QUERY_DISPLAY_CONFIG_FLAGS.QDC_ONLY_ACTIVE_PATHS, ref pathCount, paths, ref modeCount, modes) != 0)
                return result;
            
            for (var i = 0; i < pathCount; i++)
            {
                var path = paths[i];
                var sourceName = GetDisplayConfigSourceName(path.sourceInfo.adapterId, path.sourceInfo.id);
                var targetName = GetDisplayConfigTargetName(path.targetInfo.adapterId, path.targetInfo.id);

                if (!string.IsNullOrEmpty(sourceName) && !string.IsNullOrEmpty(targetName))
                {
                    result[sourceName] = targetName;
                }
            }
        }
        catch
        {
            // API失敗時 or サポート外は空で返す
        }

        return result;
    }

    private static unsafe string GetDisplayConfigSourceName(LUID adapterId, uint sourceId)
    {
        var sourceName = new DISPLAYCONFIG_SOURCE_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_SOURCE_NAME,
                size = (uint)Marshal.SizeOf<DISPLAYCONFIG_SOURCE_DEVICE_NAME>(),
                adapterId = adapterId,
                id = sourceId
            }
        };

        if (PInvoke.DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&sourceName) != 0)
            return string.Empty;

        return GetString((char*)&sourceName.viewGdiDeviceName, 32);
    }

    private static unsafe string GetDisplayConfigTargetName(LUID adapterId, uint targetId)
    {
        var targetName = new DISPLAYCONFIG_TARGET_DEVICE_NAME
        {
            header = new DISPLAYCONFIG_DEVICE_INFO_HEADER
            {
                type = DISPLAYCONFIG_DEVICE_INFO_TYPE.DISPLAYCONFIG_DEVICE_INFO_GET_TARGET_NAME,
                size = (uint)Marshal.SizeOf<DISPLAYCONFIG_TARGET_DEVICE_NAME>(),
                adapterId = adapterId,
                id = targetId
            }
        };

        if (PInvoke.DisplayConfigGetDeviceInfo((DISPLAYCONFIG_DEVICE_INFO_HEADER*)&targetName) != 0)
            return string.Empty;

        return GetString((char*)&targetName.monitorFriendlyDeviceName, 64);
    }

    private static unsafe string GetFriendlyNameViaEnum(string deviceName)
    {
        var device = new DISPLAY_DEVICEW();
        device.cb = (uint)Marshal.SizeOf(device);

        // 指定のアダプタに接続されている最初のモニターを取得（通常 index 0）
        if (PInvoke.EnumDisplayDevices(deviceName, 0, ref device, 0))
        {
            return GetString((char*)&device.DeviceString, 128);
        }
        return string.Empty;
    }

    private static unsafe string GetString(char* buffer, int length)
    {
        return GetString(new ReadOnlySpan<char>(buffer, length));
    }

    private static string GetString(ReadOnlySpan<char> buffer)
    {
        var end = buffer.IndexOf('\0');
        return end >= 0 ? new string(buffer[..end]) : new string(buffer);
    }
}
