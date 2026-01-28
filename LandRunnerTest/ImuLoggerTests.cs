using GlassBridge;
using LandRunner;

namespace LandRunnerTest;

/// <summary>
/// ImuLogger (DebugLogger) のテスト
/// デバッグログファイルの作成・書き込みを検証
/// </summary>
public class ImuLoggerTests : IDisposable
{
    private readonly string _testDirectory;

    public ImuLoggerTests()
    {
        _testDirectory = Path.Combine(Path.GetTempPath(), $"LandRunnerTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testDirectory);
    }

    public void Dispose()
    {
        try
        {
            if (Directory.Exists(_testDirectory))
            {
                Directory.Delete(_testDirectory, recursive: true);
            }
        }
        catch { /* Ignore cleanup errors */ }
    }

    [Fact]
    public void ImuLogger_Initialize_CreatesLogFile()
    {
        // Arrange & Act
        using var logger = new ImuLogger(_testDirectory);

        // Assert
        var files = Directory.GetFiles(_testDirectory);
        Assert.NotEmpty(files);
        Assert.True(files.Any(f => f.Contains("debug_") && f.EndsWith(".log")), "Debug log file should exist");
    }

    [Fact]
    public void ImuLogger_LogDebug_WritesMessage()
    {
        // Arrange
        var testMessage = "Test debug message";
        ImuLogger logger;
        string logFilePath;

        using (logger = new ImuLogger(_testDirectory))
        {
            // Act
            logger.LogDebug(testMessage);
            logFilePath = Directory.GetFiles(_testDirectory, "debug_*.log")[0];
        }

        // Force garbage collection to release file handles
        GC.Collect();
        GC.WaitForPendingFinalizers();
        System.Threading.Thread.Sleep(100);

        // Assert
        var content = File.ReadAllText(logFilePath);
        Assert.Contains(testMessage, content);
        Assert.Contains(DateTime.Now.ToString("yyyy-MM-dd"), content);
    }

    [Fact]
    public void ImuLogger_Dispose_ClosesFiles()
    {
        // Arrange
        string logPath;
        using (var logger = new ImuLogger(_testDirectory))
        {
            logger.LogDebug("Test message");
            logPath = Directory.GetFiles(_testDirectory, "debug_*.log")[0];
        }

        // Force garbage collection to release file handles
        GC.Collect();
        GC.WaitForPendingFinalizers();
        System.Threading.Thread.Sleep(100);

        // Act & Assert - Should be able to delete or write after dispose
        Assert.True(File.Exists(logPath), "Log file should exist");
        
        // Verify we can read the file (no lock)
        var content = File.ReadAllText(logPath);
        Assert.NotEmpty(content);
    }

    [Fact]
    public void ImuLogger_ThreadSafe_ConcurrentWrites()
    {
        // Arrange
        ImuLogger logger;
        string logFilePath;
        const int threadCount = 3;
        const int messagesPerThread = 10;

        using (logger = new ImuLogger(_testDirectory))
        {
            // Act
            var tasks = Enumerable.Range(0, threadCount).Select(threadId =>
                Task.Run(() =>
                {
                    for (int i = 0; i < messagesPerThread; i++)
                    {
                        logger.LogDebug($"Thread {threadId} - Message {i}");
                    }
                })
            ).ToArray();

            Task.WaitAll(tasks);
            logFilePath = Directory.GetFiles(_testDirectory, "debug_*.log")[0];
        }

        // Force garbage collection to release file handles
        GC.Collect();
        GC.WaitForPendingFinalizers();
        System.Threading.Thread.Sleep(100);

        // Assert
        var lines = File.ReadAllLines(logFilePath);
        
        // Should have at least (threadCount * messagesPerThread) lines + init messages
        Assert.True(lines.Length >= threadCount * messagesPerThread, 
            $"Expected at least {threadCount * messagesPerThread} lines, got {lines.Length}");
    }
}
