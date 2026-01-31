namespace GlassBridge.Internal.HID;

/// <summary>
/// HID�f�o�C�X�ڑ��̒��ۉ��i�f�o�C�X��ˑ��̔������b�p�[�j
/// </summary>
internal interface IHidStreamProvider : IAsyncDisposable
{
    /// <summary>
    /// �w��VID/PID�̃f�o�C�X�X�g���[�����擾
    /// </summary>
    Task<IReadOnlyList<IHidStream>> GetStreamsAsync(
        int vendorId,
        int[] productIds,
        CancellationToken cancellationToken = default);
}
