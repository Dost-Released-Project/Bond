using System.Collections.Generic;
using UnityEngine;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Pure Data] 일지 시스템에 전달될 최종 데이터 패키지
    /// </summary>
    public class JournalReport
    {
        public string Title;
        public List<string> Paragraphs = new List<string>();
        public string IconId;
        public List<JournalOption> Options = new List<JournalOption>();

        // 유저가 이 페이지에서 선택한 옵션을 임시 저장
        public JournalOption? SelectedOption;

        // 추가적인 메타데이터 (누가 보냈는지 등)
        public string ProviderId;

        // 아이템 ID, 캐릭터 ID 등 외부 로직 처리에 필요한 메타데이터
        public Dictionary<string, string> Metadata = new Dictionary<string, string>();
    }
    /// <summary>
    /// [Logic Interface] 일지 데이터를 제공하는 주체들이 구현할 인터페이스
    /// </summary>
    public interface IJournalContentProvider
    {
        /// <summary>
        /// 우선순위 (일지에서 출력될 순서, 낮을수록 먼저 출력)
        /// </summary>
        int Priority { get; }

        /// <summary>
        /// 오늘 하루 동안 누적된 데이터를 바탕으로 보고서 생성 (여러 개의 페이지로 나뉠 수 있음)
        /// </summary>
        /// <returns>표시할 내용이 없으면 빈 컬렉션 또는 null 반환</returns>
        IEnumerable<JournalReport> GetDailyReports();

        /// <summary>
        /// 다음 날을 위해 누적된 데이터 및 상태 초기화
        /// </summary>
        void ClearBuffer();
    }
}
