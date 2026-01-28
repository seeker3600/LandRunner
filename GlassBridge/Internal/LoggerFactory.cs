using Microsoft.Extensions.Logging;

namespace GlassBridge.Internal;

/// <summary>
/// GlassBridge 内部で使用するロガーファクトリ
/// </summary>
internal static class LoggerFactoryProvider
{
    private static ILoggerFactory? _instance;
    private static readonly object _lock = new();

    public static ILoggerFactory Instance
    {
        get
        {
            if (_instance == null)
            {
                lock (_lock)
                {
                    if (_instance == null)
                    {
                        // フォールバック: デフォルトで空のロガーファクトリを使用
                        _instance = new LoggerFactory();
                    }
                }
            }
            return _instance;
        }
        set
        {
            lock (_lock)
            {
                _instance = value;
            }
        }
    }
}
