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
            new ExpeditionInventory();
        public ExpeditionRegion Region { get; private set; }

        // LeeJuno 맵 시스템 호환용 passthrough — 선택된 지역의 DungeonType 을 그대로 노출
        public DungeonType DungeonType => Region != null ? Region.DungeonType : DungeonType.None;

        public IReadOnlyList<BaseCharacter> EnemyParty { get; private set; }
        // 탐사 결과 (귀환 후 마을 씬이 읽음)
        public ExpeditionOutcome Outcome { get; private set; }

        // 누적 보상 자원 (가상 계좌)
        public int AccumulatedFrontier { get; private set; } = 0;
        public int AccumulatedWood { get; private set; } = 0;
        public int AccumulatedOre { get; private set; } = 0;

        // 최근 획득 보상 (결과 패널 표시용)
        public int LastAddedFrontier { get; private set; } = 0;
        public int LastAddedWood { get; private set; } = 0;
        public int LastAddedOre { get; private set; } = 0;

        // 개척 데이터만 지급되는 상황이 있을 수 있음으로 목재와 광석에 디폴트 추가.
        // 개척 데이터와 목재 및 광석의 비율은 되도록 1/10으로 권장.
        public void AddReward(int frontier, int wood = 0, int ore = 0)
        {
            LastAddedFrontier = frontier;
            LastAddedWood = wood;
            LastAddedOre = ore;
            AccumulatedFrontier += frontier;
            AccumulatedWood += wood;
            AccumulatedOre += ore;
            Debug.Log($"<color=cyan>[ExpeditionPayload] 전투 승리 보상 적립: 개척 {frontier}, 목재 {wood}, 광석 {ore} (현재 누적: 개척 {AccumulatedFrontier}, 목재 {AccumulatedWood}, 광석 {AccumulatedOre})</color>");
        }

        public void ClearAccumulatedResources()
        {
            AccumulatedFrontier = 0;
            AccumulatedWood = 0;
            AccumulatedOre = 0;
            LastAddedFrontier = 0;
            LastAddedWood = 0;
            LastAddedOre = 0;
        }

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
            EnemyParty = null;
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