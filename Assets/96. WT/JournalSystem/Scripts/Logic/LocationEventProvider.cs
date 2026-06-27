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
        
        private class DiscoveryEvent
        {
            public string EventId;
            public string LocationName;
            public string FoundItemName;
            public string ItemId;
            public int Quantity;
            public List<JournalOption> CustomOptions;
            public List<string> CustomParagraphs;
            public string CustomTitle;
        }
        private readonly List<DiscoveryEvent> _eventBuffer = new List<DiscoveryEvent>();
        private readonly List<JournalReport> _directReports = new List<JournalReport>();

        public LocationEventProvider(JournalDataBaseSO journalDB)
        {
            _journalDB = journalDB;
        }

        public void AddDirectReports(IEnumerable<JournalReport> reports)
        {
            if (reports != null)
            {
                _directReports.AddRange(reports);
            }
        }

        /// <summary>
        /// 테스트용 데이터 설정 (이벤트 발생 시뮬레이션)
        /// </summary>
        public void SetDiscovery(
            string eventId, 
            string location, 
            string item, 
            string itemId = null, 
            int quantity = 1, 
            List<JournalOption> customOptions = null,
            List<string> customParagraphs = null,
            string customTitle = null)
        {
            _eventBuffer.Add(new DiscoveryEvent 
            { 
                EventId = eventId, 
                LocationName = location, 
                FoundItemName = item,
                ItemId = itemId,
                Quantity = quantity,
                CustomOptions = customOptions,
                CustomParagraphs = customParagraphs,
                CustomTitle = customTitle
            });
        }

        public IEnumerable<JournalReport> GetDailyReports()
        {
            foreach (var report in _directReports)
            {
                yield return report;
            }

            if (_eventBuffer.Count == 0) yield break;

            foreach (var evt in _eventBuffer)
            {
                // DB에서 해당 이벤트의 DataSO를 가져온다.
                var template = _journalDB.GetSO<JournalDataSO>(evt.EventId);
                if (template == null)
                {
                    Debug.LogWarning($"[LocationEventProvider] 일지 데이터를 찾을 수 없습니다. ID: {evt.EventId}");
                    continue;
                }

                // [Data Assembly] 템플릿과 변수를 조립하여 최종 문장 생성
                var assembledParagraphs = new List<string>();
                foreach (var para in template.Paragraphs)
                {
                    // 파라미터가 있을 때만 Format 수행 (캠핑 데이터 등 파라미터 없는 경우 대응)
                    string text = para;
                    if (para.Contains("{0}")) text = string.Format(para, evt.LocationName, evt.FoundItemName);
                    assembledParagraphs.Add(text);
                }

                var report = new JournalReport
                {
                    Title = evt.CustomTitle ?? (template.Id.Contains("CAMP") ? "캠핑 정비" : "탐색 보고"),
                    Paragraphs = evt.CustomParagraphs ?? assembledParagraphs,
                    IconId = template.EntryIconId,
                    Options = evt.CustomOptions ?? template.Options.ToList(), // 커스텀 옵션 우선 사용
                    ProviderId = "LocationEvent"
                };

                if (!string.IsNullOrEmpty(evt.ItemId))
                {
                    report.Metadata["ItemId"] = evt.ItemId;
                    report.Metadata["Quantity"] = evt.Quantity.ToString(); // Quantity 메타데이터 추가
                }

                yield return report;
            }
        }

        public void ClearBuffer()
        {
            _eventBuffer.Clear();
            _directReports.Clear();
        }
    }
}
