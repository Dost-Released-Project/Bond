using System;
using System.Collections.Generic;
using System.Linq;
using Bond.WT.Journal;
using UnityEngine;

namespace BattleSystem
{
    public class BattleRetreatController : IDisposable
    {
        private readonly JournalSystem _journalSystem;
        private readonly JournalDataBaseSO _journalDB;
        private readonly BattleRetreatJournalHandler _retreatHandler;

        public BattleRetreatController(JournalSystem journalSystem, JournalDataBaseSO journalDB, BattleRetreatJournalHandler retreatHandler)
        {
            _journalSystem = journalSystem;
            _journalDB = journalDB;
            _retreatHandler = retreatHandler;
            
            // 캠프 시스템과 동일한 방식: 지역 스코프 핸들러를 수동으로 글로벌 JournalSystem에 등록
            _journalSystem?.AddActionHandler(_retreatHandler);
        }

        public void Dispose()
        {
            _journalSystem?.RemoveActionHandler(_retreatHandler);
        }

        public void ShowRetreatConfirm()
        {
            string eventId = "EVT_BATTLE_RETREAT";
            var template = _journalDB != null ? _journalDB.GetSO<JournalDataSO>(eventId) : null;
            
            var assembledParagraphs = new List<string>();
            string iconId = "";
            List<JournalOption> options = new List<JournalOption>();

            if (template != null)
            {
                assembledParagraphs = template.Paragraphs.ToList();
                iconId = template.EntryIconId;
                if (template.Options != null)
                {
                    options.AddRange(template.Options);
                }
            }
            else
            {
                Debug.LogWarning($"[BattleRetreatController] JournalDataBaseSO에서 '{eventId}' 템플릿을 찾을 수 없습니다.");
                assembledParagraphs.Add("전투에서 퇴각하시겠습니까?");
                options.Add(new JournalOption { text = "퇴각하기", actionKey = "ACTION_RETREAT_MAP", isEnabled = true });
            }

            // "취소" 옵션 동적 추가 (actionKey를 비워두어 팝업만 닫히게 함)
            // JournalSystem은 actionKey가 없거나 매칭되는 핸들러가 없을 경우 단순히 일지창을 닫습니다.
            options.Add(new JournalOption { text = "취소", actionKey = "", isEnabled = true });

            JournalReport report = new JournalReport
            {
                Title = "전투 퇴각",
                Paragraphs = assembledParagraphs,
                IconId = iconId,
                Options = options,
                ProviderId = "BattleRetreat",
                Metadata = new Dictionary<string, string>
                {
                    { "IsPlayerWin", "false" } // 퇴각을 패배/중단으로 간주
                }
            };

            _journalSystem?.StartJournal(report);
        }
    }
}