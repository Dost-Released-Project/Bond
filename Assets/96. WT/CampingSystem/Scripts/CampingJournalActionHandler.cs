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

        public CampingJournalActionHandler(ExpeditionPayload payload, CampingSystem campingSystem)
        {
            _payload = payload;
            _campingSystem = campingSystem;
        }

        public bool CanHandle(string actionKey)
        {
            return actionKey.StartsWith("CAMP_REST_HP_") || 
                   actionKey.StartsWith("CAMP_REST_INSANITY_") || 
                   actionKey == "CAMP_END_MAINTENANCE";
        }

        public async UniTask ExecuteAction(string actionKey, JournalReport report)
        {
            if (actionKey == "CAMP_END_MAINTENANCE")
            {
                _campingSystem.EndCamping();
                return;
            }

            if (actionKey.StartsWith("CAMP_REST_HP_"))
            {
                int index = int.Parse(actionKey.Replace("CAMP_REST_HP_", ""));

                bool consumed = _payload.Supplies.ConsumeItemByType(ConsumableType.Bandage, 1);
                if (consumed)
                {
                    var chara = GetCharacter(index);
                    if (chara != null) chara.RecoverHp(20); // 기획에 따라 회복량 조절 가능
                }
            }
            else if (actionKey.StartsWith("CAMP_REST_INSANITY_"))
            {
                int index = int.Parse(actionKey.Replace("CAMP_REST_INSANITY_", ""));

                bool consumed = _payload.Supplies.ConsumeItemByType(ConsumableType.Sedative, 1);
                if (consumed)
                {
                    var chara = GetCharacter(index);
                    if (chara != null) chara.RecoverInsanity(20); // 기획에 따라 회복량 조절 가능
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
