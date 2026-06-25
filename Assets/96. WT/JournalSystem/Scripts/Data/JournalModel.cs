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
        public readonly ObservableValue<bool> IsNextButtonEnabled = new ObservableValue<bool>(true);

        public void Clear()
        {
            _reports.Clear();
            _currentReportIndex = 0;

            // 상태 강제 초기화 (이벤트 강제 발생을 위해 값 리셋)
            // ObservableValue가 동일한 값일 경우 이벤트를 발생시키지 않는 것을 우회하기 위함
            CurrentReport.Value = null;
            CurrentParagraph.Value = null; 
            CurrentOptions.Value = null;
            HasPrevPage.Value = false;
            IsLastPage.Value = false;
            IsNextButtonEnabled.Value = true;
            IsJournalComplete.Value = true;
        }

        public void SetReports(IEnumerable<JournalReport> reports)
        {
            Clear();
            
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

            // 다음 버튼 활성화 여부 결정
            if (report.Options != null && report.Options.Count > 0)
            {
                // 다른 씬 이동 기능 내제 여부 판단
                bool hasSceneTransition = false;
                foreach (var opt in report.Options)
                {
                    if (opt.actionKey == "ACTION_RETURN_MAP")
                    {
                        hasSceneTransition = true;
                        break;
                    }
                }

                if (hasSceneTransition)
                {
                    // 씬 이동 기능이 있으면 닫기 버튼 영구 비활성화
                    IsNextButtonEnabled.Value = false;
                }
                else
                {
                    // 일반 선택지가 있는 경우(마지막 페이지 포함) 선택 완료 여부에 따라 제어
                    IsNextButtonEnabled.Value = report.SelectedOption.HasValue;
                }
            }
            else
            {
                // 선택지가 없는 정보전달 챕터는 항시 활성화
                IsNextButtonEnabled.Value = true;
            }
        }
    }
}