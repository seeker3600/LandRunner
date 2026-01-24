namespace GlassBridgeTest;

using GlassBridge;
using GlassBridge.Internal;
using GlassBridge.Internal.HID;
using Xunit;

/// <summary>
/// VitureLumaDevice のテスト
/// 仕様確認テスト（簡略版）
/// </summary>
public class VitureLumaDeviceTests
{
    /// <summary>
    /// テスト用IMUデータジェネレータ
    /// </summary>
    private static async IAsyncEnumerable<ImuData> GenerateTestImuData(
        int count = 10,
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

            await Task.Delay(5, cancellationToken);
        }
    }

    /// <summary>
    /// テスト1: デバイス接続
    /// 仕様：「デバイス接続時にIsConnectedがtrueになる」
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_ShouldSucceed()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(5, ct));

        // Act
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Assert
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
    }

    /// <summary>
    /// テスト2: IMUデータストリーム読込（デバッグ版）
    /// 仕様：「IMUデータストリームを受信できる」
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task GetImuDataStreamAsync_ShouldYieldData()
    {
        // Arrange
        const int expectedCount = 1;
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(expectedCount, ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        // Act: GetImuDataStreamAsync で少なくとも 1 つのデータを取得する
        var dataList = new List<ImuData>();
        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(2));
        
        try
        {
            await foreach (var data in device.GetImuDataStreamAsync(cts.Token))
            {
                dataList.Add(data);
                Assert.NotNull(data);
                Assert.NotNull(data.EulerAngles);
                break; // 1つ取得したら終了
            }
        }
        catch (OperationCanceledException)
        {
            // キャンセルされた場合も OK（ただしデータが取得できた場合）
        }

        // Assert: 少なくとも 1 つのデータが取得できたことを確認
        Assert.True(dataList.Count >= 1, $"Expected at least 1 data, got {dataList.Count}");

        await device.DisposeAsync();
    }

    /// <summary>
    /// テスト3: Dispose時の正常終了
    /// 仕様：「DisposeasyncでIsConnectedがfalseになる」
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task DisposeAsync_ShouldDisconnect()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(5, ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        // Act
        await device.DisposeAsync();

        // Assert
        Assert.False(device.IsConnected);
    }
}

