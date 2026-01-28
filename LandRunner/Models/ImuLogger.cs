using System;
using System.IO;
using System.Text;

namespace LandRunner.Models;

/// <summary>
/// デバッグログ出力専用ユーティリティ
/// IMUデータの記録はGlassBridgeの機能を使用
/// </summary>
public class DebugLogger : IDisposable
{
    private readonly string _logFilePath;
    private StreamWriter? _logWriter;
    private readonly object _lockObject = new();
    private bool _disposed = false;

    public DebugLogger(string outputDirectory = ".")
    {
        Directory.CreateDirectory(outputDirectory);
        var timestamp = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        _logFilePath = Path.Combine(outputDirectory, $"debug_{timestamp}.log");

        try
        {
            _logWriter = new StreamWriter(
                new FileStream(_logFilePath, FileMode.Create, FileAccess.Write, FileShare.Read),
                Encoding.UTF8, bufferSize: 4096)
            {
                AutoFlush = true
            };

            LogDebug("DebugLogger initialized");
            LogDebug($"Debug log: {_logFilePath}");
        }
        catch
        {
            _logWriter?.Dispose();
            throw;
        }
    }

    /// <summary>
    /// デバッグログを出力
    /// </summary>
    public void LogDebug(string message)
    {
        if (_disposed || _logWriter == null) return;

        lock (_lockObject)
        {
            try
            {
                var logMessage = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss.fff}] {message}";
                _logWriter.WriteLine(logMessage);
                _logWriter.Flush();
                System.Diagnostics.Debug.WriteLine(logMessage);
            }
            catch { /* Ignore write errors */ }
        }
    }

    public void Dispose()
    {
        if (_disposed) return;

        lock (_lockObject)
        {
            _disposed = true;

            try
            {
                if (_logWriter != null)
                {
                    _logWriter.Flush();
                    _logWriter.Dispose();
                    _logWriter = null;
                }
            }
            catch { /* Ignore dispose errors */ }
        }
    }
}

