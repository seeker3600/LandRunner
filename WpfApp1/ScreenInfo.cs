using Vortice.DXGI;

namespace WpfApp1;

/// <summary>
/// スクリーン情報を表すモデル
/// </summary>
public class ScreenInfo
{
    public required IDXGIOutput1 Output { get; init; }
    public required string DisplayName { get; init; }
    public required int AdapterIndex { get; init; }
    public required int OutputIndex { get; init; }

    public override string ToString() => DisplayName;
}

