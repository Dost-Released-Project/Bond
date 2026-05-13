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
        private readonly IReadOnlyList<IJournalContentProvider> _providers;

        [Inject]
        public JournalSystem(JournalModel model, IReadOnlyList<IJournalContentProvider> providers)
        {
            _model = model;
            _providers = providers;
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

            // 1. 우선순위에 따라 정렬하여 리포트 수집
            var dailyReports = _providers
                .OrderBy(p => p.Priority)
                .Select(p => p.GetDailyReport())
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
        /// 플레이어가 선택지를 골랐을 때 호출
        /// </summary>
        public void SelectOption(JournalOption? option)
        {
            if (option.HasValue)
            {
                // TODO: option.Value.actionKey에 따른 결과 반영 로직
                UnityEngine.Debug.Log($"[JournalSystem] Option Selected: {option.Value.text} (ActionKey: {option.Value.actionKey})");
            }

            // 다음 사건으로 진행
            _model.TryNextReport();
        }    }
}
