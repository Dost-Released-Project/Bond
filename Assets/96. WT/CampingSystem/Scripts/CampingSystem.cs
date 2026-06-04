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

        public CampingSystem(ExpeditionPayload payload, JournalSystem journalSystem, LocationEventProvider locationProvider, JournalDataBaseSO journalDB)
        {
            _payload = payload;
            _journalSystem = journalSystem;
            _locationProvider = locationProvider;
            _journalDB = journalDB;
        }

        public void AddHander(IJournalActionHandler handler)
        {
            _journalSystem.AddActionHandler(handler);
        }

        public void GenerateCampingReport()
        {
            // [정석] 버퍼 비우고 순서대로 쌓기
            _locationProvider.ClearBuffer();
            
            // 1. 캠핑 시작 및 정비 페이지 (동적 회복 선택지 포함)
            var options = BuildRecoveryOptions();
            _locationProvider.SetDiscovery("EVT_CAMP_START", "", "", null, 1, options);
            
            // 2. 캠핑 종료 페이지 (확인 및 퇴장)
            _locationProvider.SetDiscovery("EVT_CAMP_END", "", "");
            
            // 수집 및 송출 (순차적 페이지 생성)
            _journalSystem.CollectDailyLogs();
        }

        private List<JournalOption> BuildRecoveryOptions()
        {
            var options = new List<JournalOption>();
            int hpItemCount = GetItemCountByType(ConsumableType.Bandage);
            int insanityItemCount = GetItemCountByType(ConsumableType.Sedative);

            if (_payload.Party != null)
            {
                for (int i = 0; i < _payload.Party.Count; i++)
                {
                    var chara = _payload.Party[i];
                    if (chara.Stat.current_Hp < chara.Stat.max_Hp)
                    {
                        options.Add(new JournalOption($"[{chara.Name}] HP 회복 (남은 붕대류: {hpItemCount})", $"CAMP_ACTION|{i}|{ConsumableType.Bandage}", hpItemCount > 0));
                    }

                    if (chara.Insanity > 0)
                    {
                        options.Add(new JournalOption($"[{chara.Name}] 정신력 회복 (남은 진정제류: {insanityItemCount})", $"CAMP_ACTION|{i}|{ConsumableType.Sedative}", insanityItemCount > 0));
                    }
                }
            }

            // SO 템플릿의 기본 옵션(정비 끝내기 등) 추가
            var template = _journalDB.GetSO<JournalDataSO>("EVT_CAMP_START");
            if (template != null) options.AddRange(template.Options);

            return options;
        }

        public void ExecuteFinalExit()
        {
            UnityEngine.Debug.Log("<color=cyan>[CampingSystem]</color> 최종 퇴장. Test_3_Node 씬으로 이동.");
            SceneLoader.Load("Test_3_Node");
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
