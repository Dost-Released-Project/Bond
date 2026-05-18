using System;
using System.Collections.Generic;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [RuntimeData] 일지 시스템의 런타임 상태 관리 (리포트 단위 페이지네이션)
    /// </summary>
    public class JournalModel
    {
        private readonly List<JournalReport> _reports = new List<JournalReport>();
        private int _currentReportIndex = 0;
        
        public IReadOnlyList<JournalReport> Reports => _reports;
        
        public readonly ObservableValue<string> CurrentParagraph = new ObservableValue<string>("");
        public readonly ObservableValue<IReadOnlyList<JournalOption>> CurrentOptions = new ObservableValue<IReadOnlyList<JournalOption>>(null);
        public readonly ObservableValue<bool> IsJournalComplete = new ObservableValue<bool>(true);
        public readonly ObservableValue<JournalReport> CurrentReport = new ObservableValue<JournalReport>(null);
        public readonly ObservableValue<bool> HasPrevPage = new ObservableValue<bool>(false);
        public readonly ObservableValue<bool> IsLastPage = new ObservableValue<bool>(false);

        public void SetReports(IEnumerable<JournalReport> reports)
        {
            _reports.Clear();
            _reports.AddRange(reports);
            IsJournalComplete.Value = false;
        }

        public void SaveSelectedOption(JournalOption? option)
        {
            if (_reports.Count > 0 && _currentReportIndex >= 0 && _currentReportIndex < _reports.Count)
            {
                _reports[_currentReportIndex].SelectedOption = option;
            }
        }

        public bool TryNextReport()
        {
            if (_reports.Count > 0)
            {
                _currentReportIndex = 0;
                UpdatePageState();
                return true;
            }
            
            CurrentReport.Value = null;
            IsJournalComplete.Value = true;
            return false;
        }

        public void NextPage()
        {
            if (_currentReportIndex < _reports.Count - 1)
            {
                _currentReportIndex++;
                UpdatePageState();
            }
            else
            {
                // 마지막 페이지에서 다음(또는 닫기)를 눌렀을 때
                _reports.Clear();
                TryNextReport(); // IsJournalComplete 를 true로 만들어 창을 닫게 함
            }
        }

        public void PrevPage()
        {
            if (_currentReportIndex > 0)
            {
                _currentReportIndex--;
                UpdatePageState();
            }
        }

        private void UpdatePageState()
        {
            if (_reports.Count == 0 || _currentReportIndex < 0 || _currentReportIndex >= _reports.Count) return;

            var report = _reports[_currentReportIndex];
            CurrentReport.Value = report;

            // 하나의 리포트의 여러 문단(Paragraphs)을 하나로 합쳐서 한 페이지에 표시
            CurrentParagraph.Value = string.Join("\n\n", report.Paragraphs);
            
            HasPrevPage.Value = _currentReportIndex > 0;
            
            bool isLast = _currentReportIndex == _reports.Count - 1;
            IsLastPage.Value = isLast;

            CurrentOptions.Value = report.Options;
        }
    }
}