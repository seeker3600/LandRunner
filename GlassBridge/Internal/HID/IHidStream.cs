namespace GlassBridge.Internal.HID;

/// <summary>
/// HIDストリーム操作の抽象化（HidSharpへの直接依存を排除）
/// </summary>
internal interface IHidStream : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// ストリームからデータを非同期で読み込む
    /// </summary>
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// ストリームにデータを非同期で書き込む
    /// </summary>
    Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// ストリームが開いているかを確認
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// 最大入力レポート長（Report ID を含む）
    /// </summary>
    int MaxInputReportLength { get; }

    /// <summary>
    /// 最大出力レポート長（Report ID を含む）
    /// </summary>
    int MaxOutputReportLength { get; }
}
