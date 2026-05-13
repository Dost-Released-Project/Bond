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
        public string Description;
        public Sprite Icon;
        public List<JournalOption> Options = new List<JournalOption>();
        
        // 추가적인 메타데이터 (누가 보냈는지 등)
        public string ProviderId;
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
        /// 오늘 하루 동안 누적된 데이터를 바탕으로 보고서 생성
        /// </summary>
        /// <returns>표시할 내용이 없으면 null 반환</returns>
        JournalReport GetDailyReport();

        /// <summary>
        /// 다음 날을 위해 누적된 데이터 및 상태 초기화
        /// </summary>
        void ClearBuffer();
    }
}
