using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Events;

namespace LandRunner;

/// <summary>
/// アプリケーションのエントリポイント
/// </summary>
public partial class App : Application
{
    /// <summary>
    /// アプリケーション全体で使用するロガーファクトリ
    /// </summary>
    public static ILoggerFactory LoggerFactory { get; private set; } = null!;

    static App()
    {
        InitializeLogging();
    }

    private static void InitializeLogging()
    {
        var logDir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "LandRunner", "logs");
        Directory.CreateDirectory(logDir);

        var logPath = Path.Combine(logDir, "landrunner_.log");

        // Serilog 構成（性能重視）
        Log.Logger = new LoggerConfiguration()
#if DEBUG
            .MinimumLevel.Debug()
#else
            .MinimumLevel.Information()
#endif
            .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
            .Enrich.FromLogContext()
            .WriteTo.Async(a => a.Console(
                outputTemplate: "[{Timestamp:HH:mm:ss.fff}] [{Level:u3}] {Message:lj}{NewLine}{Exception}"))
            .WriteTo.Async(a => a.File(
                path: logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                fileSizeLimitBytes: 10 * 1024 * 1024, // 10MB
                rollOnFileSizeLimit: true,
                outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                buffered: true,
                flushToDiskInterval: TimeSpan.FromSeconds(1)))
            .CreateLogger();

        // Microsoft.Extensions.Logging との統合
        LoggerFactory = new LoggerFactory().AddSerilog(Log.Logger);

        // GlassBridge 内部ロギングを統合
        GlassBridge.LoggerFactoryProvider.Instance = LoggerFactory;

        Log.Information("LandRunner 起動 (Version: {Version})",
            typeof(App).Assembly.GetName().Version);
    }

    /// <summary>
    /// 型指定のロガーを作成
    /// </summary>
    public static Microsoft.Extensions.Logging.ILogger<T> CreateLogger<T>()
        => LoggerFactory.CreateLogger<T>();

    private void Application_Exit(object sender, ExitEventArgs e)
    {
        Log.Information("LandRunner 終了");
        Log.CloseAndFlush();
    }
}
