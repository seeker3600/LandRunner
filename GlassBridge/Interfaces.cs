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

    /// <summary>
    /// デバイスに接続して IMU データを記録
    /// 取得したデバイスから GetImuDataStreamAsync() で取得したデータは自動的に記録される
    /// device.DisposeAsync() 時に自動的にメタデータも保存される
    /// </summary>
    /// <param name="outputDirectory">記録ファイルの出力先ディレクトリ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>記録付きで接続されたIMUデバイス、接続失敗時はnull</returns>
    Task<IImuDevice?> ConnectAndRecordAsync(
        string outputDirectory,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// 記録されたデータファイルから IMU デバイスを再生
    /// 実際のデバイスの代わりに、記録されたデータをストリーム配信する Mock デバイスを返す
    /// </summary>
    /// <param name="recordingDirectory">記録ファイルが保存されているディレクトリ</param>
    /// <param name="cancellationToken">キャンセルトークン</param>
    /// <returns>再生用の Mock デバイス、ファイルなし時はnull</returns>
    Task<IImuDevice?> ConnectFromRecordingAsync(
        string recordingDirectory,
        CancellationToken cancellationToken = default);
}
