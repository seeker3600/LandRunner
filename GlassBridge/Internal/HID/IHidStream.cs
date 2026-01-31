namespace GlassBridge.Internal.HID;

/// <summary>
/// HID�X�g���[������̒��ۉ��iHidSharp�ւ̒��ڈˑ���r���j
/// </summary>
internal interface IHidStream : IAsyncDisposable, IDisposable
{
    /// <summary>
    /// �X�g���[������f�[�^��񓯊��œǂݍ���
    /// </summary>
    Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken = default);

    /// <summary>
    /// �X�g���[���Ƀf�[�^��񓯊��ŏ�������
    /// </summary>
    Task WriteAsync(byte[] buffer, CancellationToken cancellationToken = default);

    /// <summary>
    /// �X�g���[�����J���Ă��邩���m�F
    /// </summary>
    bool IsOpen { get; }

    /// <summary>
    /// �ő���̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    int MaxInputReportLength { get; }

    /// <summary>
    /// �ő�o�̓��|�[�g���iReport ID ���܂ށj
    /// </summary>
    int MaxOutputReportLength { get; }
}
