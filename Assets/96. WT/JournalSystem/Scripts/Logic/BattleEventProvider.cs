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
        
        // 전투 중 참가자 수를 캐싱하기 위한 변수
        private int _cachedPlayerCount = 0;
        private int _cachedEnemyCount = 0;

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
                _battleFlowManager.OnBattleEnd += HandleBattleEnd;
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
                _battleFlowManager.OnBattleEnd -= HandleBattleEnd;
            }
            
            // 스코프 파괴 시 일지 시스템에서 등록 해제
            _journalSystem?.RemoveProvider(this);
        }

        private void HandleBattleSwitch(BaseCharacter[] players, BaseCharacter[] enemies)
        {
            // BattleManager의 방식과 동일하게 전투 상태 토글
            _isBattleActive = !_isBattleActive;

            // 배틀이 시작될 때 참가자 수 기록
            if (_isBattleActive)
            {
                _cachedPlayerCount = players?.Length ?? 0;
                _cachedEnemyCount = enemies?.Length ?? 0;
            }
            else
            {
                // 이전 방식: 버퍼에 넣어서 하루 끝에 보여줌
                // 현재는 전투 직후 팝업으로 변경되었으므로 버퍼 기록은 옵션.
                // 일별 기록도 남기고 싶다면 주석을 해제할 수 있습니다.
                // RecordBattleEnd(_cachedPlayerCount, _cachedEnemyCount);
            }
        }

        private void HandleBattleEnd(bool isPlayerWin)
        {
            // 전투가 종료되면 DB에서 템플릿(Pure Data)을 가져와 즉시 일지 시작
            string eventId = "EVT_BATTLE_END";
            var template = _journalDB != null ? _journalDB.GetSO<JournalDataSO>(eventId) : null;
            
            var assembledParagraphs = new List<string>();
            string iconId = "";
            List<JournalOption> options = new List<JournalOption>();

            if (template != null)
            {
                // [Data Assembly] 템플릿의 문장을 조립
                // 예: Paragraphs에 "{0}명의 동료들과 함께 {1}명의 적을 쓰러뜨렸다." 라고 적혀있다면 인덱스에 맞게 포맷
                foreach (var para in template.Paragraphs)
                {
                    assembledParagraphs.Add(string.Format(para, _cachedPlayerCount, _cachedEnemyCount));
                }
                iconId = template.EntryIconId;
                options = template.Options != null ? template.Options.ToList() : new List<JournalOption>();
            }
            else
            {
                // DB에 템플릿 데이터가 없는 경우를 위한 Fallback
                Debug.LogWarning($"[BattleEventProvider] JournalDataBaseSO에서 '{eventId}' 템플릿을 찾을 수 없습니다. 시트를 확인하세요.");
                assembledParagraphs.Add($"동료 {_cachedPlayerCount}명이 협력하여, {_cachedEnemyCount}명의 적과 치열한 전투를 치뤘다.");
                options.Add(new JournalOption { text = "맵으로 돌아가기", actionKey = "ACTION_RETURN_MAP" });
            }

            JournalReport report = new JournalReport
            {
                Title = isPlayerWin ? "전투 승리" : "전투 패배",
                Paragraphs = assembledParagraphs,
                IconId = iconId,
                Options = options,
                ProviderId = "BattleEvent",
                Metadata = new Dictionary<string, string>
                {
                    { "IsPlayerWin", isPlayerWin.ToString() }
                }
            };

            // 바로 일지 팝업
            _journalSystem?.StartJournal(report);
        }

        private void RecordBattleEnd(int playerCount, int enemyCount)
        {
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
                var template = _journalDB != null ? _journalDB.GetSO<JournalDataSO>(evt.EventId) : null;
                
                var assembledParagraphs = new List<string>();
                string iconId = "";
                List<JournalOption> options = new List<JournalOption>();

                if (template != null)
                {
                    foreach (var para in template.Paragraphs)
                    {
                        assembledParagraphs.Add(string.Format(para, evt.PlayerCount, evt.EnemyCount));
                    }
                    iconId = template.EntryIconId;
                    options = template.Options != null ? template.Options.ToList() : new List<JournalOption>();
                }
                else
                {
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