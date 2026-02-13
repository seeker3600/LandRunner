using System.Reflection;
using Xunit;

namespace WinUiApp1.Test;

/// <summary>
/// CaptureStatsCollector のロックフリー動作を検証する単体テスト
/// </summary>
public class CaptureStatsCollectorTest
{
    /// <summary>
    /// CaptureStatsCollector のインスタンスを作成するヘルパー
    /// （内部クラスのためリフレクションを使用）
    /// </summary>
    private static object CreateCollector()
    {
        var serviceType = typeof(ScreenCaptureService);
        var collectorType = serviceType.GetNestedType("CaptureStatsCollector", BindingFlags.NonPublic);
        Assert.NotNull(collectorType);
        
        var instance = Activator.CreateInstance(collectorType);
        Assert.NotNull(instance);
        return instance;
    }

    /// <summary>
    /// RecordFrame メソッドを呼び出すヘルパー
    /// </summary>
    private static void RecordFrame(object collector, TimeSpan processingTime)
    {
        var method = collector.GetType().GetMethod("RecordFrame");
        Assert.NotNull(method);
        method.Invoke(collector, [processingTime]);
    }

    /// <summary>
    /// ShouldReport メソッドを呼び出すヘルパー
    /// </summary>
    private static bool ShouldReport(object collector, out CaptureStats? stats)
    {
        var method = collector.GetType().GetMethod("ShouldReport");
        Assert.NotNull(method);
        
        var parameters = new object?[] { null };
        var result = method.Invoke(collector, parameters);
        
        stats = parameters[0] as CaptureStats;
        return (bool)result!;
    }

    /// <summary>
    /// Reset メソッドを呼び出すヘルパー
    /// </summary>
    private static void Reset(object collector)
    {
        var method = collector.GetType().GetMethod("Reset");
        Assert.NotNull(method);
        method.Invoke(collector, null);
    }

    [Fact]
    public void RecordFrame_SingleFrame_CountsCorrectly()
    {
        // Arrange
        var collector = CreateCollector();
        var processingTime = TimeSpan.FromMilliseconds(10);

        // Act
        RecordFrame(collector, processingTime);

        // Assert: 1秒以内はレポートが生成されない
        var shouldReport = ShouldReport(collector, out var stats);
        Assert.False(shouldReport);
        Assert.Null(stats);
    }

    [Fact]
    public void RecordFrame_MultipleFrames_AccumulatesCorrectly()
    {
        // Arrange
        var collector = CreateCollector();
        
        // Act: 60フレーム記録（各10ms）
        for (int i = 0; i < 60; i++)
        {
            RecordFrame(collector, TimeSpan.FromMilliseconds(10));
        }

        // 1秒後にレポート生成
        Thread.Sleep(1100);
        
        // Assert
        var shouldReport = ShouldReport(collector, out var stats);
        Assert.True(shouldReport);
        Assert.NotNull(stats);
        Assert.Equal(60, stats.Fps);
        Assert.InRange(stats.AvgProcessingMs, 9.5, 10.5); // 誤差範囲内
    }

    [Fact]
    public void ShouldReport_WithinOneSecond_ReturnsFalse()
    {
        // Arrange
        var collector = CreateCollector();
        RecordFrame(collector, TimeSpan.FromMilliseconds(5));

        // Act: 1秒以内
        var shouldReport = ShouldReport(collector, out var stats);

        // Assert
        Assert.False(shouldReport);
        Assert.Null(stats);
    }

    [Fact]
    public void ShouldReport_AfterOneSecond_ReturnsTrue()
    {
        // Arrange
        var collector = CreateCollector();
        RecordFrame(collector, TimeSpan.FromMilliseconds(5));

        // Act: 1秒待機
        Thread.Sleep(1100);
        var shouldReport = ShouldReport(collector, out var stats);

        // Assert
        Assert.True(shouldReport);
        Assert.NotNull(stats);
        Assert.Equal(1, stats.Fps);
    }

    [Fact]
    public void ShouldReport_ResetsCountersAfterReport()
    {
        // Arrange
        var collector = CreateCollector();
        RecordFrame(collector, TimeSpan.FromMilliseconds(10));

        // Act: 1秒後に最初のレポート
        Thread.Sleep(1100);
        var firstReport = ShouldReport(collector, out var firstStats);

        // Assert: 最初のレポートは成功
        Assert.True(firstReport);
        Assert.NotNull(firstStats);
        Assert.Equal(1, firstStats.Fps);

        // Act: 即座に2回目のレポート試行
        var secondReport = ShouldReport(collector, out var secondStats);

        // Assert: カウンターがリセットされているため失敗
        Assert.False(secondReport);
        Assert.Null(secondStats);
    }

    [Fact]
    public void Reset_ClearsAllCounters()
    {
        // Arrange
        var collector = CreateCollector();
        RecordFrame(collector, TimeSpan.FromMilliseconds(10));
        RecordFrame(collector, TimeSpan.FromMilliseconds(20));

        // Act: リセット
        Reset(collector);

        // Assert: 1秒後でもレポートなし（カウンターが0のため）
        Thread.Sleep(1100);
        var shouldReport = ShouldReport(collector, out var stats);
        Assert.False(shouldReport);
        Assert.Null(stats);
    }

    [Fact]
    public void RecordFrame_ConcurrentCalls_NoDataLoss()
    {
        // Arrange
        var collector = CreateCollector();
        const int threadCount = 10;
        const int framesPerThread = 100;
        var barrier = new Barrier(threadCount);

        // Act: 複数スレッドから同時に記録
        var threads = Enumerable.Range(0, threadCount)
            .Select(_ => new Thread(() =>
            {
                barrier.SignalAndWait(); // 全スレッドが準備できるまで待機
                for (int i = 0; i < framesPerThread; i++)
                {
                    RecordFrame(collector, TimeSpan.FromMilliseconds(5));
                }
            }))
            .ToArray();

        foreach (var thread in threads)
        {
            thread.Start();
        }

        foreach (var thread in threads)
        {
            thread.Join();
        }

        // Assert: 1秒後にレポート生成
        Thread.Sleep(1100);
        var shouldReport = ShouldReport(collector, out var stats);
        Assert.True(shouldReport);
        Assert.NotNull(stats);
        
        // 全てのフレームが正しくカウントされているか検証
        Assert.Equal(threadCount * framesPerThread, stats.Fps);
        Assert.InRange(stats.AvgProcessingMs, 4.5, 5.5);
    }

    [Fact]
    public void RecordFrame_VariousProcessingTimes_CalculatesAverageCorrectly()
    {
        // Arrange
        var collector = CreateCollector();
        var times = new[] { 5.0, 10.0, 15.0, 20.0 }; // ms
        var expectedAvg = times.Average();

        // Act
        foreach (var time in times)
        {
            RecordFrame(collector, TimeSpan.FromMilliseconds(time));
        }

        // Assert
        Thread.Sleep(1100);
        var shouldReport = ShouldReport(collector, out var stats);
        Assert.True(shouldReport);
        Assert.NotNull(stats);
        Assert.Equal(times.Length, stats.Fps);
        Assert.InRange(stats.AvgProcessingMs, expectedAvg - 0.5, expectedAvg + 0.5);
    }
}
