using System;
using System.Collections.Generic;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [RuntimeData] 일지 시스템의 런타임 상태 관리
    /// </summary>
    public class JournalModel
    {
        // 현재 일지에 표시할 리포트 큐
        private readonly Queue<JournalReport> _reportQueue = new Queue<JournalReport>();
        
        // 상태 알림을 위한 Observable 필드
        public readonly ObservableValue<string> CurrentParagraph = new ObservableValue<string>("");
        public readonly ObservableValue<IReadOnlyList<JournalOption>> CurrentOptions = new ObservableValue<IReadOnlyList<JournalOption>>(null);
        public readonly ObservableValue<bool> IsJournalComplete = new ObservableValue<bool>(true);
        public readonly ObservableValue<JournalReport> CurrentReport = new ObservableValue<JournalReport>(null);

        public void SetReports(IEnumerable<JournalReport> reports)
        {
            _reportQueue.Clear();
            foreach (var report in reports)
            {
                _reportQueue.Enqueue(report);
            }
            IsJournalComplete.Value = false;
        }

        public bool TryNextReport()
        {
            if (_reportQueue.Count > 0)
            {
                CurrentReport.Value = _reportQueue.Dequeue();
                CurrentParagraph.Value = CurrentReport.Value.Description;
                CurrentOptions.Value = CurrentReport.Value.Options;
                return true;
            }
            
            CurrentReport.Value = null;
            IsJournalComplete.Value = true;
            return false;
        }
    }
}
