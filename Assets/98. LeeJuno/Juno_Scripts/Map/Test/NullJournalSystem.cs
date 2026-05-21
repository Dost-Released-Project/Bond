using Bond.WT.Journal;

/// <summary>
/// 테스트 환경에서 JournalSystem 을 대체하는 더미.
/// EventJournalProvider 의 AddProvider / RemoveProvider 호출을 무시한다.
///
/// JournalSystem 생성자: (JournalModel, IReadOnlyList(IJournalContentProvider), IReadOnlyList(IJournalActionHandler))
/// null 전달 시 내부 null 체크를 통과하므로 안전하다.
/// </summary>
public class NullJournalSystem : JournalSystem
{
    public NullJournalSystem() : base(null, null, null) { }

    public new void AddProvider(IJournalContentProvider provider) { }
    public new void RemoveProvider(IJournalContentProvider provider) { }
}
