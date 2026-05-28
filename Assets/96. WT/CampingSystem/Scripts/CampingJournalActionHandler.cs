using Bond.Expedition;
using Bond.WT.Journal;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Bond.WT.Camping
{
    public class CampingJournalActionHandler : IJournalActionHandler
    {
        private readonly ExpeditionPayload _payload;
        private readonly CampingSystem _campingSystem;
        private readonly JournalSystem _journalSystem;

        public CampingJournalActionHandler(ExpeditionPayload payload, CampingSystem campingSystem, JournalSystem journalSystem)
        {
            _payload = payload;
            _campingSystem = campingSystem;
            _journalSystem = journalSystem;
        }

        public bool CanHandle(string actionKey)
        {
            return actionKey.StartsWith("CAMP_ACTION|") || 
                   actionKey == "CAMP_END";
        }

        public async UniTask ExecuteAction(string actionKey, JournalReport report)
        {
            Debug.Log($"[CampingJournalActionHandler] 액션 실행 시작: {actionKey}");
            if (actionKey == "CAMP_END")
            {
                _campingSystem.ExecuteFinalExit();
                return;
            }

            if (actionKey.StartsWith("CAMP_ACTION|"))
            {
                string[] parts = actionKey.Split('|');
                if (parts.Length == 3)
                {
                    int index = int.Parse(parts[1]);
                    if (System.Enum.TryParse<ConsumableType>(parts[2], out var type))
                    {
                        var chara = GetCharacter(index);
                        if (chara != null)
                        {
                            var targetSlot = _payload.Supplies.GetAll().Find(s => !s.IsEmpty && s.item is ConsumableItem c && c.consumableType == type);
                            if (targetSlot != null && targetSlot.item is ConsumableItem consumable)
                            {
                                bool consumed = _payload.Supplies.ConsumeItemByType(type, 1);
                                if (consumed)
                                {
                                    consumable.Use(chara);
                                }
                            }
                        }
                    }
                }
            }

            // OnChanged 이벤트로 인해 시스템이 자동으로 갱신하므로 수동 호출 제거
            await UniTask.CompletedTask;
        }

        private BaseCharacter GetCharacter(int index)
        {
            if (_payload == null || _payload.Party == null) return null;
            if (index >= 0 && index < _payload.Party.Count)
            {
                return _payload.Party[index];
            }
            return null;
        }
    }
}
