namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using GlassBridge.Internal.Recording;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

/// <summary>
/// IMU データ記録・再生機能のテスト
/// </summary>
public class ImuRecordingTests
{
    private readonly string _testOutputDirectory;

    public ImuRecordingTests()
    {
        _testOutputDirectory = Path.Combine(Path.GetTempPath(), $"ImuRecordingTest_{Guid.NewGuid()}");
        Directory.CreateDirectory(_testOutputDirectory);
    }

    /// <summary>
    /// テスト1: フレームレコードの JSON シリアライゼーション
    /// </summary>
    [Fact]
    public void ImuFrameRecord_SerializesCorrectly()
    {
        // Arrange
        var imuData = new ImuData
        {
            Timestamp = 12345,
            MessageCounter = 42,
            Quaternion = new Quaternion(1.0f, 0.1f, 0.2f, 0.3f),
            EulerAngles = new EulerAngles(10.5f, 20.5f, 30.5f)
        };
        var rawBytes = new byte[] { 0xFF, 0xFC, 0x00, 0x00, 0x1E, 0x00 };

        // Act
        var frameRecord = ImuFrameRecord.FromImuData(imuData, rawBytes);
        var jsonLine = frameRecord.ToJsonLine();
        var parsedRecord = ImuFrameRecord.FromJsonLine(jsonLine);

        // Assert
        Assert.NotEmpty(jsonLine);
        Assert.Equal(imuData.Timestamp, parsedRecord.Timestamp);
        Assert.Equal(imuData.MessageCounter, parsedRecord.MessageCounter);
        Assert.Equal(imuData.Quaternion.W, parsedRecord.Quaternion.W);
        Assert.Equal(imuData.EulerAngles.Roll, parsedRecord.EulerAngles.Roll);
    }

    /// <summary>
    /// テスト2: メタデータのシリアライゼーション
    /// </summary>
    [Fact]
    public void ImuRecordingSession_SerializesMetadata()
    {
        // Arrange
        var session = ImuRecordingSession.CreateNew(frameCount: 100, sampleRate: 50);

        // Act
        var json = session.ToJson();
        var deserializedSession = ImuRecordingSession.FromJson(json);

        // Assert
        Assert.Equal(100, deserializedSession.FrameCount);
        Assert.Equal(50, deserializedSession.SampleRate);
        Assert.Equal("jsonl", deserializedSession.Format);
        Assert.NotNull(deserializedSession.RecordedAt);
    }

    /// <summary>
    /// テスト3: ファイル形式の可読性確認
    /// </summary>
    [Fact]
    public void RecordingFormat_IsHumanReadable()
    {
        // Arrange
        var imuData = new ImuData
        {
            Timestamp = 1000,
            MessageCounter = 0,
            Quaternion = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f),
            EulerAngles = new EulerAngles(0.0f, 0.0f, 0.0f)
        };
        var rawBytes = new byte[] { 0xFF, 0xFC, 0x00, 0x00 };

        // Act
        var frameRecord = ImuFrameRecord.FromImuData(imuData, rawBytes);
        var jsonLine = frameRecord.ToJsonLine();

        // Assert: JSONが人間が読める形式か確認
        Assert.Contains("timestamp", jsonLine);
        Assert.Contains("messageCounter", jsonLine);
        Assert.Contains("quaternion", jsonLine);
        Assert.Contains("eulerAngles", jsonLine);
        Assert.Contains("1000", jsonLine);  // timestamp値
        Assert.DoesNotContain("\\u", jsonLine);  // Unicode エスケープがないことを確認
    }

    /// <summary>
    /// テスト4: メタデータの JSON 形式確認
    /// </summary>
    [Fact]
    public void ImuRecordingSession_JsonIsReadable()
    {
        // Arrange
        var session = ImuRecordingSession.CreateNew(frameCount: 100, sampleRate: 50);

        // Act
        var json = session.ToJson();

        // Assert
        Assert.Contains("recordedAt", json);
        Assert.Contains("frameCount", json);
        Assert.Contains("sampleRate", json);
        Assert.Contains("format", json);
        Assert.Contains("jsonl", json);
        Assert.Contains("100", json);  // frameCount値
    }

    /// <summary>
    /// テスト5: RecordingSessionのラウンドトリップシリアライゼーション
    /// </summary>
    [Fact]
    public void ImuRecordingSession_RoundTripSerialization()
    {
        // Arrange
        var original = ImuRecordingSession.CreateNew(frameCount: 50, sampleRate: 100);
        var recordedAtBefore = original.RecordedAt;

        // Act
        var json = original.ToJson();
        var deserialized = ImuRecordingSession.FromJson(json);

        // Assert
        Assert.Equal(original.FrameCount, deserialized.FrameCount);
        Assert.Equal(original.SampleRate, deserialized.SampleRate);
        Assert.Equal(original.Format, deserialized.Format);
        Assert.Equal(recordedAtBefore, deserialized.RecordedAt);
    }

    /// <summary>
    /// テスト6: ファイルへの直接書き込みと読み込み
    /// </summary>
    [Fact]
    public void RecordingSession_WriteAndReadFromFile()
    {
        // Arrange
        var session = ImuRecordingSession.CreateNew(frameCount: 75, sampleRate: 25);
        var filePath = Path.Combine(_testOutputDirectory, "session.json");

        // Act
        var json = session.ToJson();
        File.WriteAllText(filePath, json);
        
        var fileContent = File.ReadAllText(filePath);
        var loadedSession = ImuRecordingSession.FromJson(fileContent);

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.Equal(session.FrameCount, loadedSession.FrameCount);
        Assert.Equal(session.SampleRate, loadedSession.SampleRate);
    }

    /// <summary>
    /// テスト7: JSON Lines フォーマットの複数フレーム保存
    /// </summary>
    [Fact]
    public void ImuFrameRecord_MultipleFramesWriteToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testOutputDirectory, "multi_frames.jsonl");
        var frames = new[]
        {
            new ImuData
            {
                Timestamp = 0,
                MessageCounter = 0,
                Quaternion = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f),
                EulerAngles = new EulerAngles(0.0f, 0.0f, 0.0f)
            },
            new ImuData
            {
                Timestamp = 10,
                MessageCounter = 1,
                Quaternion = new Quaternion(1.0f, 0.01f, 0.02f, 0.03f),
                EulerAngles = new EulerAngles(1.0f, 2.0f, 3.0f)
            },
            new ImuData
            {
                Timestamp = 20,
                MessageCounter = 2,
                Quaternion = new Quaternion(1.0f, 0.02f, 0.04f, 0.06f),
                EulerAngles = new EulerAngles(2.0f, 4.0f, 6.0f)
            }
        };

        // Act
        using (var writer = new StreamWriter(filePath))
        {
            foreach (var frame in frames)
            {
                var record = ImuFrameRecord.FromImuData(frame, Array.Empty<byte>());
                writer.WriteLine(record.ToJsonLine());
            }
        }

        var readLines = File.ReadAllLines(filePath);

        // Assert
        Assert.Equal(3, readLines.Length);
        
        // 各行をパースして検証
        for (int i = 0; i < readLines.Length; i++)
        {
            var record = ImuFrameRecord.FromJsonLine(readLines[i]);
            Assert.Equal(frames[i].Timestamp, record.Timestamp);
            Assert.Equal(frames[i].MessageCounter, record.MessageCounter);
            Assert.Equal(frames[i].EulerAngles.Roll, record.EulerAngles.Roll);
        }
    }

    /// <summary>
    /// テスト8: RecordedHidStream の基本的な動作確認
    /// </summary>
    [Fact]
    public async Task RecordedHidStream_BasicFunctionality()
    {
        // Arrange
        var testData = new ImuData[]
        {
            new ImuData
            {
                Timestamp = 0,
                MessageCounter = 0,
                Quaternion = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f),
                EulerAngles = new EulerAngles(0.0f, 0.0f, 0.0f)
            },
            new ImuData
            {
                Timestamp = 10,
                MessageCounter = 1,
                Quaternion = new Quaternion(1.0f, 0.01f, 0.02f, 0.03f),
                EulerAngles = new EulerAngles(1.0f, 2.0f, 3.0f)
            }
        };

        var framesPath = Path.Combine(_testOutputDirectory, "replay_test.jsonl");
        var metadataPath = Path.Combine(_testOutputDirectory, "replay_test_metadata.json");

        // フレームファイルを作成
        using (var writer = new StreamWriter(framesPath))
        {
            foreach (var frame in testData)
            {
                var record = ImuFrameRecord.FromImuData(frame, new byte[] { 0xFF, 0xFC });
                writer.WriteLine(record.ToJsonLine());
            }
        }

        // メタデータファイルを作成
        var metadata = ImuRecordingSession.CreateNew(frameCount: 2, sampleRate: 100);
        File.WriteAllText(metadataPath, metadata.ToJson());

        // Act
        var replayStream = new RecordedHidStream(framesPath, metadataPath);
        var buffer = new byte[64];
        
        int frameCount = 0;
        while (await replayStream.ReadAsync(buffer, 0, buffer.Length) > 0 && frameCount < 10)
        {
            frameCount++;
        }

        await replayStream.DisposeAsync();

        // Assert
        Assert.True(frameCount > 0, "Should have replayed at least one frame");
    }

    /// <summary>
    /// テスト9: 低速データストリーム受信のシミュレーション（パフォーマンス計測用）
    /// 遅延なしでデータを受信する
    /// </summary>
    [Fact]
    public void ImuFrameRecord_HighSpeedDataGeneration()
    {
        // Arrange: 遅延なしでデータを生成
        var imuDataEnumerable = GenerateTestImuData(count: 100, delayMs: 0);
        var recordList = new List<ImuFrameRecord>();

        // Act
        var syncEnumerator = imuDataEnumerable.GetAsyncEnumerator();
        var task = Task.Run(async () =>
        {
            while (await syncEnumerator.MoveNextAsync())
            {
                var record = ImuFrameRecord.FromImuData(syncEnumerator.Current, new byte[] { 0xFF, 0xFC });
                recordList.Add(record);
            }
        });
        task.Wait(5000);

        // Assert
        Assert.Equal(100, recordList.Count);
        Assert.NotEmpty(recordList);
    }

    /// <summary>
    /// テスト10: 実デバイス相当の遅延でのデータ受信
    /// 10ms の遅延（実デバイスのデータ送信タイミングをシミュレート）
    /// </summary>
    [Fact]
    public void ImuFrameRecord_WithDeviceLatencySimulation()
    {
        // Arrange: 10ms 遅延でデータを生成（実デバイスシミュレーション）
        var imuDataEnumerable = GenerateTestImuData(count: 20, delayMs: 10);
        var recordList = new List<ImuFrameRecord>();

        // Act
        var syncEnumerator = imuDataEnumerable.GetAsyncEnumerator();
        var task = Task.Run(async () =>
        {
            while (await syncEnumerator.MoveNextAsync())
            {
                var record = ImuFrameRecord.FromImuData(syncEnumerator.Current, new byte[] { 0xFF, 0xFC });
                recordList.Add(record);
            }
        });
        task.Wait(10000);

        // Assert
        Assert.True(recordList.Count > 0, "Should have received some frames");
        Assert.True(recordList.Count <= 20, "Should not exceed requested count");
        
        // タイムスタンプが順序通りであることを確認
        for (int i = 1; i < recordList.Count; i++)
        {
            Assert.True(recordList[i].Timestamp >= recordList[i - 1].Timestamp, 
                "Timestamps should be in order");
        }
    }

    private IAsyncEnumerable<ImuData> GenerateTestImuData(int count = 20, int delayMs = 0)
    {
        return GenerateTestImuDataAsync();

        async IAsyncEnumerable<ImuData> GenerateTestImuDataAsync()
        {
            for (uint i = 0; i < count; i++)
            {
                yield return new ImuData
                {
                    Timestamp = i * 10,
                    MessageCounter = (ushort)i,
                    Quaternion = new Quaternion(1.0f, 0.01f * i, 0.02f * i, 0.03f * i),
                    EulerAngles = new EulerAngles(i * 1.0f, i * 2.0f, i * 3.0f)
                };
                
                if (delayMs > 0)
                {
                    await Task.Delay(delayMs);
                }
            }
        }
    }

    private MockHidStreamProvider CreateMockProvider(int delayMs = 0)
    {
        // MockHidStreamProvider は Func を期待
        return new MockHidStreamProvider(_ => GenerateTestImuData(count: 20, delayMs: delayMs));
    }

    public void Dispose()
    {
        // テスト終了時にディレクトリを削除
        try
        {
            if (Directory.Exists(_testOutputDirectory))
                Directory.Delete(_testOutputDirectory, true);
        }
        catch
        {
            // クリーンアップ失敗は無視
        }
    }
}
