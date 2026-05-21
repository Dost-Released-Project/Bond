using Cysharp.Threading.Tasks;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Logic Interface] 일지 선택지 결과(액션)를 외부 시스템에서 반영하기 위한 인터페이스
    /// </summary>
    public interface IJournalActionHandler
    {
        /// <summary>
        /// 이 핸들러가 해당 actionKey를 처리할 책임이 있는지 여부를 반환합니다.
        /// </summary>
        bool CanHandle(string actionKey);

        /// <summary>
        /// 실제 외부 시스템 로직(아이템 지급, 스탯 변화 등)을 실행합니다.
        /// </summary>
        UniTask ExecuteAction(string actionKey, JournalReport report);
    }
}
