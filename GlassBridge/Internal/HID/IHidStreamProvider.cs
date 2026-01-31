namespace GlassBridge.Internal.HID;

/// <summary>
/// HIDデバイス接続の抽象化（デバイス非依存の薄いラッパー）
/// </summary>
internal interface IHidStreamProvider : IAsyncDisposable
{
    /// <summary>
    /// 指定VID/PIDのデバイスストリームを取得
    /// </summary>
    Task<IReadOnlyList<IHidStream>> GetStreamsAsync(
        int vendorId,
        int[] productIds,
        CancellationToken cancellationToken = default);
}
