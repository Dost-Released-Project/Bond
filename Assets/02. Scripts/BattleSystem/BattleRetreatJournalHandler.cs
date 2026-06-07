using System;
using System.Threading;
using BattleSystem.Interface;
using Bond.WT.Journal;
using Cysharp.Threading.Tasks;

namespace BattleSystem
{
    public class BattleRetreatJournalHandler : IJournalActionHandler
    {
        private readonly IBattleFlowManager _battleFlowManager;
        private readonly JournalSystem _journalSystem;

        public BattleRetreatJournalHandler(IBattleFlowManager battleFlowManager, JournalSystem journalSystem)
        {
            _battleFlowManager = battleFlowManager;
            _journalSystem = journalSystem;
        }

        public bool CanHandle(string actionKey)
        {
            return actionKey == "ACTION_RETREAT_MAP";
        }

        public async UniTask ExecuteAction(string actionKey, JournalReport report)
        {
            if (actionKey == "ACTION_RETREAT_MAP")
            {
                // 퇴각 시 열려 있는 일지 세션(상태) 강제 종료
                _journalSystem?.ClearJournal();
                
                // 퇴각 처리 트리거.
                // BattleFlowManager가 ProcessBattleEndAsync(false)를 호출하여 
                // 전투 상태 정리 및 이벤트 해제 등을 정상적으로 수행한 뒤 OnBattleEnd를 발생시킵니다.
                _battleFlowManager.HandleRetreat();
                await UniTask.CompletedTask;
            }
        }
    }
}