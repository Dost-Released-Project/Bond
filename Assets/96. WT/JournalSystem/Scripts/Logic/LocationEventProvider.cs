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
            public int Quantity; // 새로 추가
        }
        private readonly List<DiscoveryEvent> _eventBuffer = new List<DiscoveryEvent>();

        public LocationEventProvider(JournalDataBaseSO journalDB)
        {
            _journalDB = journalDB;
        }

        /// <summary>
        /// 테스트용 데이터 설정 (이벤트 발생 시뮬레이션)
        /// </summary>
        public void SetDiscovery(string eventId, string location, string item, string itemId = null, int quantity = 1)
        {
            _eventBuffer.Add(new DiscoveryEvent 
            { 
                EventId = eventId, 
                LocationName = location, 
                FoundItemName = item,
                ItemId = itemId,
                Quantity = quantity
            });
        }

        public IEnumerable<JournalReport> GetDailyReports()
        {
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
                    assembledParagraphs.Add(string.Format(para, evt.LocationName, evt.FoundItemName));
                }

                var report = new JournalReport
                {
                    Title = "탐색 보고",
                    Paragraphs = assembledParagraphs,
                    IconId = template.EntryIconId,
                    Options = template.Options.ToList(),
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
        }
    }
}
