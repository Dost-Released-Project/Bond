using System.Collections.Generic;
using System.Linq;
using VContainer;
using VContainer.Unity;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [System] 일지 생성 및 선택지 결과 처리 로직
    /// </summary>
    public class JournalSystem : IInitializable
    {
        private readonly JournalModel _model;
        private readonly List<IJournalContentProvider> _providers = new List<IJournalContentProvider>();
        private readonly IReadOnlyList<IJournalActionHandler> _actionHandlers;

        [Inject]
        public JournalSystem(JournalModel model, IReadOnlyList<IJournalContentProvider> providers, IReadOnlyList<IJournalActionHandler> actionHandlers)
        {
            _model = model;
            if (providers != null)
            {
                _providers.AddRange(providers);
            }
            _actionHandlers = actionHandlers;
        }

        public void AddProvider(IJournalContentProvider provider)
        {
            if (!_providers.Contains(provider))
                _providers.Add(provider);
        }

        public void RemoveProvider(IJournalContentProvider provider)
        {
            if (_providers.Contains(provider))
                _providers.Remove(provider);
        }

        public void Initialize()
        {
            // 초기화 로직
        }

        /// <summary>
        /// 모든 Provider로부터 데이터를 수집하여 하루 일지를 생성
        /// </summary>
        public void CollectDailyLogs()
        {
            if (_providers == null || _providers.Count == 0) return;

            // 1. 우선순위에 따라 정렬하여 리포트 수집 (각 Provider가 여러 리포트를 반환할 수 있으므로 SelectMany 사용)
            var dailyReports = _providers
                .OrderBy(p => p.Priority)
                .SelectMany(p => p.GetDailyReports() ?? Enumerable.Empty<JournalReport>())
                .Where(r => r != null)
                .ToList();

            if (dailyReports.Count > 0)
            {
                // 2. 모델에 데이터 전달 및 시작
                _model.SetReports(dailyReports);
                _model.TryNextReport();
            }

            // 3. 수집 완료 후 모든 Provider 버퍼 초기화
            foreach (var provider in _providers)
            {
                provider.ClearBuffer();
            }
        }

        /// <summary>
        /// 다음 페이지(또는 다음 리포트)로 이동
        /// </summary>
        public void NextPage()
        {
            // 마지막 페이지에서 다음(또는 닫기)를 누른 경우, 그동안 저장된 모든 선택 결과를 일괄 실행
            if (_model.IsLastPage.Value)
            {
                ExecuteAllDeferredOptions();
            }

            _model.NextPage();
        }

        private void ExecuteAllDeferredOptions()
        {
            foreach (var report in _model.Reports)
            {
                if (report.SelectedOption.HasValue)
                {
                    var opt = report.SelectedOption.Value;
                    
                    bool handled = false;
                    
                    // 등록된 외부 핸들러들에게 액션 실행 위임
                    if (_actionHandlers != null)
                    {
                        foreach (var handler in _actionHandlers)
                        {
                            if (handler.CanHandle(opt.actionKey))
                            {
                                handler.ExecuteAction(opt.actionKey);
                                handled = true;
                            }
                        }
                    }

                    // 처리할 핸들러가 없는 경우의 기본 로그
                    if (!handled)
                    {
                        UnityEngine.Debug.LogWarning($"[JournalSystem] '{opt.actionKey}' 액션을 처리할 IJournalActionHandler를 찾을 수 없습니다.");
                    }
                }
            }
        }

        /// <summary>
        /// 이전 페이지로 이동
        /// </summary>
        public void PrevPage()
        {
            _model.PrevPage();
        }

        /// <summary>
        /// 플레이어가 선택지를 골랐을 때 호출 (결과 지연 실행)
        /// </summary>
        public void SelectOption(JournalOption? option)
        {
            // 1. 현재 리포트에 선택 결과를 임시 저장만 함 (실행은 마지막 장에서)
            _model.SaveSelectedOption(option);

            // 2. 선택지를 골랐으면 다음 사건으로 진행 (페이지네이션 완료 처리)
            NextPage();
        }    
    }
}