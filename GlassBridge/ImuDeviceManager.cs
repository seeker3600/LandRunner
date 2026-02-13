namespace GlassBridge;

using GlassBridge.Internal.HID;
using GlassBridge.Internal.Recording;
using GlassBridge.Internal.VitureLuma;
using Microsoft.Extensions.Logging;

/// <summary>
/// IMUデバイスマネージャーの実装
/// デバイス接続、記録、再生機能を提供
/// </summary>
public sealed class ImuDeviceManager : IImuDeviceManager
{
    private static readonly ILogger<ImuDeviceManager> _logger 
        = LoggerFactoryProvider.Instance.CreateLogger<ImuDeviceManager>();

    private bool _disposed;
    private RecordingHidStreamProvider? _recordingProvider;

    /// <summary>
    /// VITURE Lumaデバイスに通常接続
    /// </summary>
    public async Task<IImuDevice?> ConnectAsync(CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImuDeviceManager));

        var device = await VitureLumaDevice.ConnectAsync(cancellationToken);
        
        if (device != null)
            _logger.LogInformation("デバイスに接続しました");
        else
            _logger.LogWarning("デバイスが見つかりませんでした");
        
        return device;
    }

    /// <summary>
    /// デバイスに接続して IMU データを記録
    /// 取得したデバイスから GetImuDataStreamAsync() で取得したデータは自動的に記録される
    /// device.DisposeAsync() 時に自動的にメタデータも保存される
    /// </summary>
    /// <param name="recordingFilePath">記録ファイルパス（.jsonl）</param>
    public async Task<IImuDevice?> ConnectAndRecordAsync(
        string recordingFilePath,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImuDeviceManager));

        if (string.IsNullOrWhiteSpace(recordingFilePath))
            throw new ArgumentException("Recording file path must not be null or empty", nameof(recordingFilePath));

        // 前回の記録セッションを終了
        if (_recordingProvider != null)
            await _recordingProvider.DisposeAsync();

        // 基本的なHIDストリームプロバイダーを作成
        var baseProvider = new HidStreamProvider();

        // 記録機能でラップ
        _recordingProvider = new RecordingHidStreamProvider(baseProvider, recordingFilePath);

        // デバイスに接続
        var device = await VitureLumaDevice.ConnectWithProviderAsync(_recordingProvider, cancellationToken);
        
        if (device != null)
            _logger.LogInformation("デバイスに接続しました（記録モード: {RecordingFilePath}）", recordingFilePath);
        else
            _logger.LogWarning("デバイスが見つかりませんでした");
        
        return device;
    }

    /// <summary>
    /// 記録されたデータファイルから IMU デバイスを再生
    /// 実際のデバイスの代わりに、記録されたデータをストリーム配信する Mock デバイスを返す
    /// </summary>
    /// <param name="recordingFilePath">記録ファイルパス（.jsonl）</param>
    public async Task<IImuDevice?> ConnectReplayAsync(
        string recordingFilePath,
        CancellationToken cancellationToken = default)
    {
        if (_disposed)
            throw new ObjectDisposedException(nameof(ImuDeviceManager));

        if (string.IsNullOrWhiteSpace(recordingFilePath))
            throw new ArgumentException("Recording file path must not be null or empty", nameof(recordingFilePath));

        if (!File.Exists(recordingFilePath))
            throw new FileNotFoundException($"Recording file not found: {recordingFilePath}");

        // 再生プロバイダーを作成
        var replayProvider = new ReplayHidStreamProvider(recordingFilePath);

        // Mock デバイスとして再生
        var device = await VitureLumaDevice.ConnectWithProviderAsync(replayProvider, cancellationToken);

        if (device != null)
            _logger.LogInformation("記録データから再生を開始しました: {RecordingFilePath}", recordingFilePath);
        else
            _logger.LogWarning("記録データの読み込みに失敗しました: {RecordingFilePath}", recordingFilePath);

        return device;
    }

    public void Dispose()
    {
        if (_disposed)
            return;

        if (_recordingProvider != null)
        {
            try
            {
                _recordingProvider.DisposeAsync().GetAwaiter().GetResult();
            }
            catch
            {
                // ディスポーズ時のエラーは無視
            }
        }

        _disposed = true;
    }
}
