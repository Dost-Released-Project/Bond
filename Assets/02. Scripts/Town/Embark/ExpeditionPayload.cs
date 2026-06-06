using System.Collections.Generic;
using BattleSystem;
using Bond.Embark;
using UnityEngine;

namespace Bond.Expedition
{
    public enum ExpeditionOutcome
    {
        None = 0,
        Success,
        Failure
    }
    
    public enum DungeonType
    {
        None = 0,
        All,
        Forest,
        Ruin,
    }
    
    // 마을 씬 → 탐사 씬으로 넘기는 데이터 컨테이너
    public class ExpeditionPayload : IPartyController
    {
        // IPartyController ====
        private PartyController _partyController = new PartyController();
        public List<BaseCharacter> GetCurrentParty() => _partyController.GetCurrentParty();
        public bool IsInParty(BaseCharacter character) => _partyController.IsInParty(character);
        public bool IsFull() => _partyController.IsFull();
        public bool TryAddMember(BaseCharacter character) => _partyController.TryAddMember(character);
        public bool RemoveMember(BaseCharacter character) => _partyController.RemoveMember(character);
        void IPartyController.Clear() => _partyController.Clear();
        // =====================
        
        public IReadOnlyList<BaseCharacter> Party => GetCurrentParty();
        public ExpeditionInventory Supplies { get; private set; } =
            new ExpeditionInventory(ExpeditionInventory.PeekInventoryCapacity("exp_inv", 2));
        public ExpeditionRegion Region { get; private set; }

        // LeeJuno 맵 시스템 호환용 passthrough — 선택된 지역의 DungeonType 을 그대로 노출
        public DungeonType DungeonType => Region != null ? Region.DungeonType : DungeonType.None;

        public IReadOnlyList<BaseCharacter> EnemyParty { get; private set; }
        // 탐사 결과 (귀환 후 마을 씬이 읽음)
        public ExpeditionOutcome Outcome { get; private set; }

        public void SetContents(
            IReadOnlyList<BaseCharacter> party,
            ExpeditionInventory supplies,
            ExpeditionRegion region)
        {
            //Party = party;
            Supplies = supplies;
            Region = region;
        }

        public void SetSuplies(ExpeditionInventory supplies)
        {
            Supplies = supplies;
        }

        public void SetResult(ExpeditionOutcome outcome)
        {
            Outcome = outcome;
        }

        public void Clear()
        {
            _partyController.Clear();
            Region = null;
            Outcome = ExpeditionOutcome.None;
        }

        public override string ToString()
        {
            string str = "";
            foreach (var c in Party)
            {
                str += $"{c.Name}\n";
            }

            foreach (var slot in Supplies.GetAll())
            {
                str += $"{slot.item.itemName}\n";
            }

            return str;
        }

        public void SetEnemy(IReadOnlyList<BaseCharacter> enemyParty)
        {
            EnemyParty = enemyParty;
        }
    }
}