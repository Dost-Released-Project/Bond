using System;
using System.Collections.Generic;
using System.Linq;
using BattleSystem.Interface;
using PipeLine;
using UnityEngine;
using VContainer.Unity;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Logic] 배틀 종료 시 이벤트(또는 주요 상황)를 수집하여 일지로 발행하는 Provider
    /// </summary>
    public class BattleEventProvider : IJournalContentProvider, IStartable, IDisposable
    {
        public int Priority => 30; // LocationEventProvider(50)보다 높은 우선순위

        private readonly JournalDataBaseSO _journalDB;
        private readonly IBattleFlowManager _battleFlowManager;
        private readonly JournalSystem _journalSystem;

        private class BattleResultEvent
        {
            public string EventId;
            public int PlayerCount;
            public int EnemyCount;
        }

        private readonly List<BattleResultEvent> _eventBuffer = new List<BattleResultEvent>();
        private bool _isBattleActive = false;

        public BattleEventProvider(JournalDataBaseSO journalDB, IBattleFlowManager battleFlowManager, JournalSystem journalSystem)
        {
            _journalDB = journalDB;
            _battleFlowManager = battleFlowManager;
            _journalSystem = journalSystem;

            if (_battleFlowManager != null)
            {
                // 배틀 매니저의 시작/종료 이벤트를 구독 (Observer)
                // "Blind Logic" 원칙에 따라 배틀 시스템에 관여하지 않고 관찰만 수행
                _battleFlowManager.OnBattle += HandleBattleSwitch;
            }
        }

        public void Start()
        {
            // 스코프 생성 시 일지 시스템에 자신을 등록
            _journalSystem?.AddProvider(this);
        }

        public void Dispose()
        {
            if (_battleFlowManager != null)
            {
                _battleFlowManager.OnBattle -= HandleBattleSwitch;
            }
            
            // 스코프 파괴 시 일지 시스템에서 등록 해제
            _journalSystem?.RemoveProvider(this);
        }

        private void HandleBattleSwitch(BaseCharacter[] players, BaseCharacter[] enemies)
        {
            // BattleManager의 방식과 동일하게 전투 상태 토글
            _isBattleActive = !_isBattleActive;

            // 배틀이 종료되었을 때 (true -> false로 바뀌는 시점) 일지 데이터 버퍼에 기록
            if (!_isBattleActive)
            {
                RecordBattleEnd(players?.Length ?? 0, enemies?.Length ?? 0);
            }
        }

        private void RecordBattleEnd(int playerCount, int enemyCount)
        {
            // 테스트를 위해 "BATTLE_END" 라는 EventId 부여
            // 향후 JournalData에 BATTLE_END ID로 데이터를 추가하면 템플릿 사용 가능
            _eventBuffer.Add(new BattleResultEvent 
            {
                EventId = "EVT_BATTLE_END", 
                PlayerCount = playerCount,
                EnemyCount = enemyCount
            });
        }

        public IEnumerable<JournalReport> GetDailyReports()
        {
            if (_eventBuffer.Count == 0) yield break;

            foreach (var evt in _eventBuffer)
            {
                // DB에서 템플릿(Pure Data)을 가져옴
                var template = _journalDB != null ? _journalDB.GetSO<JournalDataSO>(evt.EventId) : null;
                
                var assembledParagraphs = new List<string>();
                string iconId = "";
                List<JournalOption> options = new List<JournalOption>();

                if (template != null)
                {
                    // [Data Assembly] 템플릿의 문장을 조립
                    // 예: Paragraphs에 "{0}명의 동료들과 함께 {1}마리의 적을 처치했다." 라고 적혀있다면 인덱스에 맞게 포맷
                    foreach (var para in template.Paragraphs)
                    {
                        assembledParagraphs.Add(string.Format(para, evt.PlayerCount, evt.EnemyCount));
                    }
                    iconId = template.EntryIconId;
                    options = template.Options != null ? template.Options.ToList() : new List<JournalOption>();
                }
                else
                {
                    // DB에 BATTLE_END 템플릿 데이터가 없는 경우를 위한 Fallback 기본 텍스트
                    assembledParagraphs.Add($"동료 {evt.PlayerCount}명이 협력하여, {evt.EnemyCount}명의 적과 치열한 전투를 치뤘다.");
                }

                yield return new JournalReport
                {
                    Title = "전투 기록",
                    Paragraphs = assembledParagraphs,
                    IconId = iconId,
                    Options = options,
                    ProviderId = "BattleEvent"
                };
            }
        }

        public void ClearBuffer()
        {
            _eventBuffer.Clear();
        }
    }
}