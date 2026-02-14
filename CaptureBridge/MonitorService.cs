using System.Collections.ObjectModel;
using System.Runtime.InteropServices;
using Windows.Graphics;
using Windows.Graphics.Display;
using Windows.Win32;
using Windows.Win32.Devices.Display;
using Windows.Win32.Foundation;
using Windows.Win32.Graphics.Gdi;

namespace CaptureBridge;

/// <summary>
/// 接続されているモニター情報
/// </summary>
public record DisplayMonitorInfo(
    DisplayId Id, // IntPtrの方がいいかもしれん
    //string DeviceName,
    string FriendlyName);

/// <summary>
/// マルチモニター情報を取得するサービス
/// </summary>
public static class MonitorService
{
    public static bool ExcludeFromCapture(IntPtr hwnd)
    { 
        return PInvoke.SetWindowDisplayAffinity(new HWND(hwnd), Windows.Win32.UI.WindowsAndMessaging.WINDOW_DISPLAY_AFFINITY.WDA_EXCLUDEFROMCAPTURE);
    }

    public static ReadOnlyCollection<DisplayMonitorInfo> GetAllMonitors()
    {
        var friendlyNamesCache = GetMonitorFriendlyNamesCache();
        var ids = DisplayServices.FindAll();
        var results = new List<DisplayMonitorInfo>();

        foreach (var displayId in ids)
        {
            var info = new MONITORINFOEXW();
            info.monitorInfo.cbSize = (uint)Marshal.SizeOf<MONITORINFOEXW>();

            string friendlyName;
            if (PInvoke.GetMonitorInfo((HMONITOR)displayId.Value, ref info.monitorInfo))
            {
                var deviceName = GetString(info.szDevice.AsSpan());
                friendlyName = friendlyNamesCache.TryGetValue(deviceName, out var name)
                    ? name
                    : deviceName;
            }
            else
            {
                friendlyName = string.Empty;
            }

            results.Add(new DisplayMonitorInfo(
                displayId,
                friendlyName));
        }

        return results.AsReadOnly();
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
