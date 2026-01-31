using Microsoft.Extensions.Logging;

namespace GlassBridge.Internal;

/// <summary>
/// GlassBridge �����Ŏg�p���郍�K�[�t�@�N�g��
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
                        // �t�H�[���o�b�N: �f�t�H���g�ŋ�̃��K�[�t�@�N�g�����g�p
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
