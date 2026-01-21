namespace GlassBridge;

/// <summary>
/// IMUデバイス管理インターフェース（テスト可能性を考慮）
/// </summary>
public interface IImuDevice : IAsyncDisposable
{
    /// <summary>
    /// IMUデータストリームを取得
    /// </summary>
    IAsyncEnumerable<ImuData> GetImuDataStreamAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// デバイスが接続されているかを確認
    /// </summary>
    bool IsConnected { get; }
}

/// <summary>
/// IMUデバイスマネージャーのインターフェース
/// </summary>
public interface IImuDeviceManager : IDisposable
{
    /// <summary>
    /// VITURE Lumaデバイスを検出して接続
    /// </summary>
    /// <returns>接続されたIMUデバイス、接続失敗時はnull</returns>
    Task<IImuDevice?> ConnectAsync(CancellationToken cancellationToken = default);
}
