namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal;
using GlassBridge.Utils;
using GlassBridge.Internal.Recording;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

/// <summary>
/// HID データ記録・再生機能のテスト（VitureLuma非依存）
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
    /// テスト1: HIDフレームレコードの JSON シリアライゼーション
    /// </summary>
    [Fact]
    public void HidFrameRecord_SerializesCorrectly()
    {
        // Arrange
        var rawBytes = new byte[] { 0xFF, 0xFC, 0x00, 0x00, 0x1E, 0x00 };
        var timestamp = 12345L;

        // Act
        var frameRecord = HidFrameRecord.Create(rawBytes, timestamp, streamId: 0);
        var json = frameRecord.ToJson();
        var parsedRecord = HidFrameRecord.FromJson(json);

        // Assert
        Assert.NotEmpty(json);
        Assert.Equal(timestamp, parsedRecord.Timestamp);
        Assert.Equal("frame", parsedRecord.Type);
        Assert.Equal(Convert.ToBase64String(rawBytes), parsedRecord.RawBytes);
        Assert.Equal(rawBytes, parsedRecord.DecodeRawBytes());
    }

    /// <summary>
    /// テスト2: メタデータのシリアライゼーション
    /// </summary>
    [Fact]
    public void HidRecordingMetadata_SerializesMetadata()
    {
        // Arrange
        var metadata = HidRecordingMetadata.Create(streamCount: 2);

        // Act
        var json = metadata.ToJson();
        var deserializedMetadata = HidRecordingMetadata.FromJson(json);

        // Assert
        Assert.Equal(2, deserializedMetadata.StreamCount);
        Assert.Equal("metadata", deserializedMetadata.Type);
        Assert.Equal(2, deserializedMetadata.FormatVersion);
        Assert.NotNull(deserializedMetadata.RecordedAt);
    }

    /// <summary>
    /// テスト3: ファイル形式の可読性確認
    /// </summary>
    [Fact]
    public void RecordingFormat_IsHumanReadable()
    {
        // Arrange
        var rawBytes = new byte[] { 0xFF, 0xFC, 0x00, 0x00 };
        var timestamp = 1000L;

        // Act
        var frameRecord = HidFrameRecord.Create(rawBytes, timestamp, streamId: 0);
        var json = frameRecord.ToJson();

        // Assert: JSONが人間が読める形式か確認
        Assert.Contains("timestamp", json);
        Assert.Contains("rawBytes", json);
        Assert.Contains("type", json);
        Assert.Contains("frame", json);
        Assert.Contains("1000", json);  // timestamp値
        Assert.DoesNotContain("\\u", json);  // Unicode エスケープがないことを確認
    }

    /// <summary>
    /// テスト4: メタデータの JSON 形式確認
    /// </summary>
    [Fact]
    public void HidRecordingMetadata_JsonIsReadable()
    {
        // Arrange
        var metadata = HidRecordingMetadata.Create(streamCount: 2);

        // Act
        var json = metadata.ToJson();

        // Assert
        Assert.Contains("recordedAt", json);
        Assert.Contains("streamCount", json);
        Assert.Contains("formatVersion", json);
        Assert.Contains("type", json);
        Assert.Contains("metadata", json);
        Assert.Contains("2", json);  // streamCount値
    }

    /// <summary>
    /// テスト5: RecordingMetadataのラウンドトリップシリアライゼーション
    /// </summary>
    [Fact]
    public void HidRecordingMetadata_RoundTripSerialization()
    {
        // Arrange
        var original = HidRecordingMetadata.Create(streamCount: 2);
        var recordedAtBefore = original.RecordedAt;

        // Act
        var json = original.ToJson();
        var deserialized = HidRecordingMetadata.FromJson(json);

        // Assert
        Assert.Equal(original.StreamCount, deserialized.StreamCount);
        Assert.Equal(original.FormatVersion, deserialized.FormatVersion);
        Assert.Equal(original.Type, deserialized.Type);
        Assert.Equal(recordedAtBefore, deserialized.RecordedAt);
    }

    /// <summary>
    /// テスト6: ファイルへの直接書き込みと読み込み
    /// </summary>
    [Fact]
    public void RecordingMetadata_WriteAndReadFromFile()
    {
        // Arrange
        var metadata = HidRecordingMetadata.Create(streamCount: 2);
        var filePath = Path.Combine(_testOutputDirectory, "metadata.json");

        // Act
        var json = metadata.ToJson();
        File.WriteAllText(filePath, json);
        
        var fileContent = File.ReadAllText(filePath);
        var loadedMetadata = HidRecordingMetadata.FromJson(fileContent);

        // Assert
        Assert.True(File.Exists(filePath));
        Assert.Equal(metadata.StreamCount, loadedMetadata.StreamCount);
        Assert.Equal(metadata.FormatVersion, loadedMetadata.FormatVersion);
    }

    /// <summary>
    /// テスト7: JSON Lines フォーマットの複数フレーム保存
    /// </summary>
    [Fact]
    public void HidFrameRecord_MultipleFramesWriteToFile()
    {
        // Arrange
        var filePath = Path.Combine(_testOutputDirectory, "multi_frames.jsonl");
        var testFrames = new[]
        {
            (timestamp: 0L, data: new byte[] { 0xFF, 0xFC, 0x00 }),
            (timestamp: 10L, data: new byte[] { 0xFF, 0xFC, 0x01 }),
            (timestamp: 20L, data: new byte[] { 0xFF, 0xFC, 0x02 })
        };

        // Act
        using (var writer = new StreamWriter(filePath))
        {
            // 1行目: メタデータ
            var metadata = HidRecordingMetadata.Create(streamCount: 1);
            writer.WriteLine(metadata.ToJson());

            // 2行目以降: フレーム
            foreach (var (timestamp, data) in testFrames)
            {
                var record = HidFrameRecord.Create(data, timestamp, streamId: 0);
                writer.WriteLine(record.ToJson());
            }
        }

        var readLines = File.ReadAllLines(filePath);

        // Assert
        Assert.Equal(4, readLines.Length);  // メタデータ + 3フレーム
        
        // メタデータを検証
        var loadedMetadata = HidRecordingMetadata.FromJson(readLines[0]);
        Assert.Equal(1, loadedMetadata.StreamCount);

        // 各フレームを検証
        for (int i = 0; i < testFrames.Length; i++)
        {
            var record = HidFrameRecord.FromJson(readLines[i + 1]);
            Assert.Equal(testFrames[i].timestamp, record.Timestamp);
            Assert.Equal(testFrames[i].data, record.DecodeRawBytes());
        }
    }

    /// <summary>
    /// テスト8: ReplayHidStream の基本的な動作確認
    /// </summary>
    [Fact]
    public async Task ReplayHidStream_BasicFunctionality()
    {
        // Arrange
        var testFrames = new[]
        {
            (timestamp: 0L, data: new byte[] { 0xFF, 0xFC, 0x00 }),
            (timestamp: 10L, data: new byte[] { 0xFF, 0xFC, 0x01 })
        };

        var recordingPath = Path.Combine(_testOutputDirectory, "replay_test.jsonl");

        // 記録ファイルを作成
        using (var writer = new StreamWriter(recordingPath))
        {
            // 1行目: メタデータ
            var metadata = HidRecordingMetadata.Create(streamCount: 1);
            writer.WriteLine(metadata.ToJson());

            // 2行目以降: フレーム
            foreach (var (timestamp, data) in testFrames)
            {
                var record = HidFrameRecord.Create(data, timestamp, streamId: 0);
                writer.WriteLine(record.ToJson());
            }
        }

        // Act
        var replayStream = new ReplayHidStream(recordingPath, streamId: 0);
        var buffer = new byte[64];
        
        int frameCount = 0;
        while (await replayStream.ReadAsync(buffer, 0, buffer.Length) > 0 && frameCount < 10)
        {
            frameCount++;
        }

        await replayStream.DisposeAsync();

        // Assert
        Assert.Equal(testFrames.Length, frameCount);
    }

    /// <summary>
    /// テスト9: HIDバイト列の高速生成
    /// </summary>
    [Fact]
    public void HidFrameRecord_HighSpeedDataGeneration()
    {
        // Arrange: 100フレーム生成
        var recordList = new List<HidFrameRecord>();

        // Act
        for (int i = 0; i < 100; i++)
        {
            var rawBytes = new byte[] { 0xFF, 0xFC, (byte)i };
            var timestamp = i * 10L;
            var record = HidFrameRecord.Create(rawBytes, timestamp, streamId: 0);
            recordList.Add(record);
        }

        // Assert
        Assert.Equal(100, recordList.Count);
        Assert.NotEmpty(recordList);
        
        
        // 各レコードを検証
        for (int i = 0; i < recordList.Count; i++)
        {
            Assert.Equal(i * 10L, recordList[i].Timestamp);
        }
    }
}

