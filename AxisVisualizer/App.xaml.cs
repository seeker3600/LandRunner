using System;
using System.IO;
using System.Windows;
using Microsoft.Extensions.Logging;
using Serilog;

namespace LandRunner
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        public static ILoggerFactory LoggerFactory { get; set; }

        static App()
        {
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "LandRunner");
            Directory.CreateDirectory(appDataPath);

            // Serilog 構成
            Log.Logger = new LoggerConfiguration()
                .MinimumLevel.Debug()
                .WriteTo.Console()
                .WriteTo.File(
                    path: Path.Combine(appDataPath, $"debug_{DateTime.Now:yyyyMMdd_HHmmss}.log"),
                    outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff}] [{Level:u3}] [{SourceContext}] {Message:lj}{NewLine}{Exception}",
                    rollingInterval: RollingInterval.Day)
                .CreateLogger();

            // Microsoft.Extensions.Logging を Serilog と統合
            LoggerFactory = new LoggerFactory()
                .AddSerilog(Log.Logger);

            // GlassBridge 内部で使用するロガーファクトリを設定
            GlassBridge.LoggerFactoryProvider.Instance = LoggerFactory;
        }

        public static ILogger<T> CreateLogger<T>() => LoggerFactory.CreateLogger<T>();

        private void Application_Exit(object sender, ExitEventArgs e)
        {
            Log.CloseAndFlush();
        }
    }
}

