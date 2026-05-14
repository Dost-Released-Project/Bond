using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Logic] 장소 탐색 결과를 일지로 변환하는 샘플 Provider
    /// </summary>
    public class LocationEventProvider : IJournalContentProvider
    {
        // IJournalContentProvider 구현
        public int Priority => 50;

        // 조립에 필요한 데이터 (실제로는 생성자 주입이나 다른 시스템에서 설정됨)
        private readonly JournalDataBaseSO _journalDB;
        private string _locationName = "숲";
        private string _foundItemName = "낡은 지도";
        private bool _hasEventOccurred = false;
        private string _currentEventId;

        public LocationEventProvider(JournalDataBaseSO journalDB)
        {
            _journalDB = journalDB;
        }

        /// <summary>
        /// 테스트용 데이터 설정 (이벤트 발생 시뮬레이션)
        /// </summary>
        public void SetDiscovery(string eventId, string location, string item)
        {
            _currentEventId = eventId;
            _locationName = location;
            _foundItemName = item;
            _hasEventOccurred = true;
        }

        public JournalReport GetDailyReport()
        {
            if (!_hasEventOccurred || string.IsNullOrEmpty(_currentEventId)) return null;

            // DB에서 해당 이벤트의 DataSO를 가져온다.
            var template = _journalDB.GetSO<JournalDataSO>(_currentEventId);
            if (template == null)
            {
                Debug.LogWarning($"[LocationEventProvider] 일지 데이터를 찾을 수 없습니다. ID: {_currentEventId}");
                return null;
            }

            // [Data Assembly] 템플릿과 변수를 조립하여 최종 문장 생성
            // 템플릿 예시: "오늘 {0}에서 {1}을(를) 발견했습니다."
            string rawText = template.Paragraphs.Count > 0 ? template.Paragraphs[0] : "데이터 없음";
            string finalDescription = string.Format(rawText, _locationName, _foundItemName);

            return new JournalReport
            {
                Title = "탐색 보고",
                Description = finalDescription,
                IconId = template.EntryIconId,
                Options = template.Options.ToList(),
                ProviderId = "LocationEvent"
            };
        }

        public void ClearBuffer()
        {
            _hasEventOccurred = false;
        }
    }
}
