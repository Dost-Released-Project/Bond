using System;
using System.Collections.Generic;
using System.Linq;
using Bond.Expedition;
using Bond.WT.Journal;
using UnityEngine;

namespace Bond.WT.Camping
{
    public class CampingSystem
    {
        private readonly ExpeditionPayload _payload;
        private readonly JournalSystem _journalSystem;
        private readonly LocationEventProvider _locationProvider;
        private readonly JournalDataBaseSO _journalDB;
        private readonly EventLogAccumulator _logAccumulator;

        public CampingSystem(
            ExpeditionPayload payload, 
            JournalSystem journalSystem, 
            LocationEventProvider locationProvider, 
            JournalDataBaseSO journalDB,
            EventLogAccumulator logAccumulator)
        {
            _payload = payload;
            _journalSystem = journalSystem;
            _locationProvider = locationProvider;
            _journalDB = journalDB;
            _logAccumulator = logAccumulator;
        }

        public void AddHander(IJournalActionHandler handler)
        {
            _journalSystem.AddActionHandler(handler);
        }

        public void GenerateCampingReport()
        {
            // [정석] 버퍼 비우고 순서대로 쌓기
            _locationProvider.ClearBuffer();

            // 0. 이전 탐사 일지 기록 추가
            if (_logAccumulator != null && _logAccumulator.HasLogs)
            {
                _locationProvider.AddDirectReports(_logAccumulator.AllLogs);
            }
            
            bool hasBandageTarget = false;
            bool hasSedativeTarget = false;

            if (_payload.Party != null)
            {
                foreach (var chara in _payload.Party)
                {
                    if (chara.Stat.current_Hp < chara.Stat.max_Hp) hasBandageTarget = true;
                    if (chara.Insanity > 0) hasSedativeTarget = true;
                }
            }

            // 1. 캠핑 붕대 정비 페이지 (대상자가 있을 때만)
            if (hasBandageTarget)
            {
                var bandageOptions = BuildBandageOptions();
                _locationProvider.SetDiscovery(
                    "EVT_CAMP_START", 
                    "", 
                    "", 
                    null, 
                    1, 
                    bandageOptions, 
                    new List<string> { "붕대를 사용하여 부상당한 아군을 치료할 수 있습니다. 치료하지 않고 방치하는 대원은 스트레스를 받습니다." }, 
                    "캠핑 정비 - 붕대 치료"
                );
            }
            
            // 2. 캠핑 진정제 정비 페이지 (대상자가 있을 때만)
            if (hasSedativeTarget)
            {
                var sedativeOptions = BuildSedativeOptions();
                _locationProvider.SetDiscovery(
                    "EVT_CAMP_START", 
                    "", 
                    "", 
                    null, 
                    1, 
                    sedativeOptions, 
                    new List<string> { "진정제를 사용하여 대원들의 스트레스를 해소할 수 있습니다. 진정제를 사용하지 않고 방치하는 대원은 추가 스트레스를 받습니다." }, 
                    "캠핑 정비 - 정신력 회복"
                );
            }
            
            // 3. 캠핑 종료 페이지 (확인 및 퇴장)
            _locationProvider.SetDiscovery(
                "EVT_CAMP_END", 
                "", 
                "", 
                null, 
                1, 
                null, 
                new List<string> { "정비를 모두 마치고 캠프를 퇴장합니다." }, 
                "캠핑 정비 완료"
            );
            
            // 수집 및 송출 (순차적 페이지 생성)
            _journalSystem.CollectDailyLogs();
        }

        private List<JournalOption> BuildBandageOptions()
        {
            var options = new List<JournalOption>();
            int hpItemCount = GetItemCountByType(ConsumableType.Bandage);

            if (_payload.Party != null)
            {
                for (int i = 0; i < _payload.Party.Count; i++)
                {
                    var chara = _payload.Party[i];
                    if (chara.Stat.current_Hp < chara.Stat.max_Hp)
                    {
                        options.Add(new JournalOption($"[{chara.Name}] HP 회복 (남은 붕대: {hpItemCount})", $"CAMP_ACTION|{i}|{ConsumableType.Bandage}", hpItemCount > 0));
                        options.Add(new JournalOption($"[{chara.Name}] 치료하지 않고 방치 (스트레스 증가)", $"CAMP_SKIP|{i}|{ConsumableType.Bandage}", true));
                    }
                }
            }

            // SO 템플릿의 기본 옵션(정비 끝내기 등) 추가
            var template = _journalDB.GetSO<JournalDataSO>("EVT_CAMP_START");
            if (template != null) options.AddRange(template.Options);

            return options;
        }

        private List<JournalOption> BuildSedativeOptions()
        {
            var options = new List<JournalOption>();
            int insanityItemCount = GetItemCountByType(ConsumableType.Sedative);

            if (_payload.Party != null)
            {
                for (int i = 0; i < _payload.Party.Count; i++)
                {
                    var chara = _payload.Party[i];
                    if (chara.Insanity > 0)
                    {
                        options.Add(new JournalOption($"[{chara.Name}] 정신력 회복 (남은 진정제: {insanityItemCount})", $"CAMP_ACTION|{i}|{ConsumableType.Sedative}", insanityItemCount > 0));
                        options.Add(new JournalOption($"[{chara.Name}] 진정제 사용하지 않고 방치 (스트레스 증가)", $"CAMP_SKIP|{i}|{ConsumableType.Sedative}", true));
                    }
                }
            }

            // SO 템플릿의 기본 옵션 추가
            var template = _journalDB.GetSO<JournalDataSO>("EVT_CAMP_START");
            if (template != null) options.AddRange(template.Options);

            return options;
        }

        public void ExecuteFinalExit()
        {
            UnityEngine.Debug.Log("<color=cyan>[CampingSystem]</color> 최종 퇴장. 씬 언로드를 위해 StageCompletionChannel 호출.");
            StageResult result = new StageResult
            {
                IsSuccess = true,
                IsGameOver = false,
                IsBattleTriggered = false,
                RewardIds = new List<string>()
            };
            StageCompletionChannel.Invoke(result);
        }

        private int GetItemCountByType(ConsumableType type)
        {
            if (_payload?.Supplies == null) return 0;
            return _payload.Supplies.GetAll()
                .Where(s => !s.IsEmpty && s.item is ConsumableItem c && c.consumableType == type)
                .Sum(s => s.quantity);
        }
    }
}
