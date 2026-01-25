namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using Xunit;

/// <summary>
/// MockHidStream と MockMcuStream の動作テスト
/// </summary>
public class MockStreamTests
{
    /// <summary>
    /// テスト用IMUデータジェネレータ
    /// </summary>
    private static async IAsyncEnumerable<ImuData> GenerateTestImuData(
        int count = 1,
        [System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        for (int i = 0; i < count; i++)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return new ImuData
            {
                Quaternion = new GlassBridge.Quaternion(0.707f, 0f, 0f, 0.707f),
                EulerAngles = new EulerAngles(0f, 45f, 0f),
                Timestamp = (uint)(1000 + i),
                MessageCounter = (ushort)i
            };

            await Task.Delay(1, cancellationToken);
        }
    }

    /// <summary>
    /// テスト1: MockMcuStream がコマンド受け取り後に ACK を返す
    /// </summary>
    [Fact]
    public async Task MockMcuStream_ShouldReturnAckAfterWrite()
    {
        // Arrange
        var mcuStream = new MockMcuStream();
        var buffer = new byte[64];

        // Act: Write
        await mcuStream.WriteAsync(buffer, CancellationToken.None);

        // Read
        int bytesRead = await mcuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead > 0, "MCU stream should return ACK packet");
        Assert.Equal(0xFF, buffer[0]);
        Assert.Equal(0xFD, buffer[1]);

        await mcuStream.DisposeAsync();
    }

    /// <summary>
    /// テスト2: MockHidStream が IMU データを返す
    /// </summary>
    [Fact]
    public async Task MockHidStream_ShouldReturnImuData()
    {
        // Arrange
        var imuStream = new MockHidStream(GenerateTestImuData(1, CancellationToken.None), CancellationToken.None);
        var buffer = new byte[64];

        // Act
        int bytesRead = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead > 0, "IMU stream should return data");
        Assert.Equal(0xFF, buffer[0]);
        // ヘッダ確認: IMU データ
        if (bytesRead >= 2)
        {
            Assert.Equal(0xFC, buffer[1]);
        }

        await imuStream.DisposeAsync();
    }

    /// <summary>
    /// テスト3: MockHidStreamProvider が 2 つのストリームを返す
    /// </summary>
    [Fact]
    public async Task MockHidStreamProvider_ShouldReturnTwoStreams()
    {
        // Arrange
        var provider = new MockHidStreamProvider(ct => GenerateTestImuData(1, ct));

        // Act
        var streams = await provider.GetStreamsAsync(CancellationToken.None);

        // Assert
        Assert.Equal(2, streams.Count);
        
        // 最初はMCU、次はIMU
        var mcuStream = streams[0] as MockMcuStream;
        var imuStream = streams[1] as MockHidStream;

        Assert.NotNull(mcuStream);
        Assert.NotNull(imuStream);

        await provider.DisposeAsync();
    }

    /// <summary>
    /// テスト4: MockHidStream から複数回読み込み
    /// </summary>
    [Fact]
    public async Task MockHidStream_ShouldSupportMultipleReads()
    {
        // Arrange
        var imuStream = new MockHidStream(GenerateTestImuData(2, CancellationToken.None), CancellationToken.None);
        var buffer = new byte[64];

        // Act: 1回目の読み込み
        int bytesRead1 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // 2回目の読み込み
        int bytesRead2 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead1 > 0, "First read should return data");
        Assert.True(bytesRead2 > 0, "Second read should return data");

        await imuStream.DisposeAsync();
    }

    /// <summary>
    /// テスト5: MockHidStream データストリーム終了後は 0 を返す
    /// </summary>
    [Fact]
    public async Task MockHidStream_ShouldReturnZeroWhenDataExhausted()
    {
        // Arrange
        var imuStream = new MockHidStream(GenerateTestImuData(1, CancellationToken.None), CancellationToken.None);
        var buffer = new byte[64];

        // Act: 1回目の読み込み
        int bytesRead1 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // 2回目以降の読み込み
        int bytesRead2 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);
        int bytesRead3 = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead1 > 0, "First read should return data");
        Assert.Equal(0, bytesRead2); // データ終了後
        Assert.Equal(0, bytesRead3); // 継続して 0

        await imuStream.DisposeAsync();
    }

    /// <summary>
    /// テスト6: MockHidStream がパケットをシリアライズ
    /// </summary>
    [Fact]
    public async Task MockHidStream_ShouldSerializePacketCorrectly()
    {
        // Arrange
        var imuStream = new MockHidStream(GenerateTestImuData(1, CancellationToken.None), CancellationToken.None);
        var buffer = new byte[64];

        // Act
        int bytesRead = await imuStream.ReadAsync(buffer, 0, buffer.Length, CancellationToken.None);

        // Assert
        Assert.True(bytesRead >= 20, "Packet should be at least 20 bytes");
        Assert.Equal(0xFF, buffer[0]); // Header byte 0
        Assert.Equal(0xFC, buffer[1]); // Header byte 1 (IMU data)

        await imuStream.DisposeAsync();
    }
}
