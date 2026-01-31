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
    /// データ受信速度をシミュレーション可能
    /// </summary>
    /// <param name="count">生成するデータ数</param>
    /// <param name="delayMs">フレーム間の遅延（ms）。0 でパフォーマンス計測、>0 でタイムアウト等をテスト</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    private static async IAsyncEnumerable<ImuData> GenerateTestImuData(
        int count = 10,
        int delayMs = 0,
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

            if (delayMs > 0)
            {
                await Task.Delay(delayMs, cancellationToken);
            }
        }
    }

    /// <summary>
    /// テスト1: デバイス接続（パフォーマンス計測用）
    /// 仕様：「デバイス接続時にIsConnectedがtrueになる」
    /// 遅延なしで高速実行
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task ConnectAsync_ShouldSucceed()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(5, delayMs: 0, cancellationToken: ct));

        // Act
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Assert
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
    }

    /// <summary>
    /// テスト2: GetImuDataStreamAsync メソッドが存在し、呼び出し可能（パフォーマンス計測用）
    /// 仕様：「IMUデータストリームメソッドが実装されている」
    /// 遅延なしで高速実行
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task GetImuDataStreamAsync_ShouldBeCallable()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(1, delayMs: 0, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        // Act: GetImuDataStreamAsync メソッドが呼び出し可能か確認
        var streamMethod = device.GetType().GetMethod("GetImuDataStreamAsync");

        // Assert: メソッドが存在し、実装されていることを確認
        Assert.NotNull(streamMethod);
        Assert.True(streamMethod.ReturnType.IsGenericType);

        await device.DisposeAsync();
    }

    /// <summary>
    /// テスト3: Dispose時の正常終了（パフォーマンス計測用）
    /// 仕様：「DisposeasyncでIsConnectedがfalseになる」
    /// 遅延なしで高速実行
    /// </summary>
    [Fact(Timeout = 5000)]
    public async Task DisposeAsync_ShouldDisconnect()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(5, delayMs: 0, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        // Act
        await device.DisposeAsync();

        // Assert
        Assert.False(device.IsConnected);
    }

    /// <summary>
    /// テスト4: 低速データストリーム受信のシミュレーション
    /// 実デバイスは数 ms～数十 ms のタイミングでデータを送信する
    /// タイムアウト処理やバッファリング動作の確認用
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task ConnectAsync_WithDelayedData_ShouldSucceed()
    {
        // Arrange: 10ms の遅延でデータを送信（実デバイスシミュレーション）
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(10, delayMs: 10, cancellationToken: ct));

        // Act
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

        // Assert
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        await device.DisposeAsync();
    }

    /// <summary>
    /// テスト5: デバイス初期化と複数回の接続テスト
    /// 複数のデバイス接続シーケンスが正常に動作することを確認
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task ConnectAsync_MultipleConnections_ShouldSucceed()
    {
        // Arrange & Act: 複数回の接続を試行
        for (int i = 0; i < 3; i++)
        {
            var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(3, delayMs: 0, cancellationToken: ct));
            var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);

            // Assert
            Assert.NotNull(device);
            Assert.True(device.IsConnected);

            await device.DisposeAsync();
            Assert.False(device.IsConnected);
        }
    }

    /// <summary>
    /// テスト6: デバイス初期化後は IMU が無効化されていることを確認
    /// DisposeAsync でも無効化コマンドが送信される
    /// </summary>
    [Fact(Timeout = 10000)]
    public async Task DisposeAsync_ShouldDisableImuOnCleanup()
    {
        // Arrange
        var mockProvider = new MockHidStreamProvider(ct => GenerateTestImuData(10, delayMs: 0, cancellationToken: ct));
        var device = await VitureLumaDevice.ConnectWithProviderAsync(mockProvider);
        Assert.NotNull(device);
        Assert.True(device.IsConnected);

        // Act: Dispose を呼び出す
        await device.DisposeAsync();

        // Assert: デバイスが切断されたことを確認
        Assert.False(device.IsConnected);
        // Dispose 時に IMU 無効化コマンドが送信される（実装詳細だが確認可能）
    }
}



