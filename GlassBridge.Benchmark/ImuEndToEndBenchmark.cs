namespace GlassBridgeBenchmark;

using BenchmarkDotNet.Attributes;
using GlassBridge;
using GlassBridge.Internal;
using GlassBridge.Utils;
using System.Runtime.CompilerServices;

/// <summary>
/// IMUデータ受信～クライアントがyieldを受け取るまでのエンドツーエンド性能を計測
/// 
/// フロー:
///   HID読み取り → パケット解析 → ImuData変換 → yield → クライアント受信
/// </summary>
[MemoryDiagnoser]
[RankColumn]
public class ImuEndToEndBenchmark
{
    private const int DataCount = 1000;

    private ImuData[] _preGeneratedData = null!;

    [GlobalSetup]
    public void Setup()
    {
        // 事前にテスト用ImuDataを生成
        _preGeneratedData = new ImuData[DataCount];
        for (int i = 0; i < DataCount; i++)
        {
            _preGeneratedData[i] = new ImuData
            {
                Quaternion = new Quaternion(1.0f, 0.0f, 0.0f, 0.0f),
                EulerAngles = new EulerAngles(
                    Roll: 10.5f + i * 0.1f,
                    Pitch: -5.2f + i * 0.05f,
                    Yaw: 45.0f + i * 0.2f
                ),
                Timestamp = (uint)(10000 + i),
                MessageCounter = (ushort)i
            };
        }
    }

    /// <summary>
    /// エンドツーエンド: MockHidStreamProvider → VitureLumaDevice → GetImuDataStreamAsync → クライアント受信
    /// 実際のデータフロー全体を計測（デバイス接続～全データ受信まで）
    /// </summary>
    [Benchmark(Baseline = true)]
    public async Task<int> EndToEnd_FullPipeline()
    {
        var provider = new MockHidStreamProvider(_ => GenerateDataAsync(_preGeneratedData));

        await using var device = await VitureLumaDevice.ConnectWithProviderAsync(provider);
        if (device == null)
            return 0;

        int count = 0;
        await foreach (var data in device.GetImuDataStreamAsync())
        {
            count++;
            if (count >= DataCount)
                break;
        }

        return count;
    }

    /// <summary>
    /// 比較用: MockImuDevice経由（HID層をバイパス）
    /// オーバーヘッドの内訳を理解するための参考値
    /// </summary>
    [Benchmark]
    public async Task<int> EndToEnd_MockDevice_NoHidLayer()
    {
        await using var device = new MockImuDevice(_ => GenerateDataAsync(_preGeneratedData));

        int count = 0;
        await foreach (var data in device.GetImuDataStreamAsync())
        {
            count++;
            if (count >= DataCount)
                break;
        }

        return count;
    }

    /// <summary>
    /// 単一データ受信のレイテンシ計測（フルパイプライン）
    /// </summary>
    [Benchmark]
    public async Task<ImuData?> SingleData_FullPipeline()
    {
        var singleData = new[] { _preGeneratedData[0] };
        var provider = new MockHidStreamProvider(_ => GenerateDataAsync(singleData));

        await using var device = await VitureLumaDevice.ConnectWithProviderAsync(provider);
        if (device == null)
            return null;

        await foreach (var data in device.GetImuDataStreamAsync())
        {
            return data;
        }

        return null;
    }

    /// <summary>
    /// 単一データ受信のレイテンシ計測（MockImuDevice経由）
    /// </summary>
    [Benchmark]
    public async Task<ImuData?> SingleData_MockDevice()
    {
        var singleData = new[] { _preGeneratedData[0] };
        await using var device = new MockImuDevice(_ => GenerateDataAsync(singleData));

        await foreach (var data in device.GetImuDataStreamAsync())
        {
            return data;
        }

        return null;
    }

    /// <summary>
    /// テストデータを非同期ストリームとして生成
    /// </summary>
    private static async IAsyncEnumerable<ImuData> GenerateDataAsync(
        ImuData[] dataArray,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        foreach (var data in dataArray)
        {
            if (cancellationToken.IsCancellationRequested)
                yield break;

            yield return data;
            await Task.Yield(); // 非同期コンテキストスイッチをシミュレート
        }
    }
}
