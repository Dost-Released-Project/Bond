using System.Collections.Generic;
using BattleSystem;
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

    public class ExpeditionPayload
    {
        // 마을 씬 → 탐사 씬으로 넘기는 데이터 컨테이너

        public IReadOnlyList<BaseCharacter> Party { get; private set; } = new List<BaseCharacter>();
        public ExpeditionInventory Supplies { get; private set; } =
            new ExpeditionInventory(ExpeditionInventory.PeekInventoryCapacity("exp_inv", 2));
        public DungeonType DungeonType { get; private set; }

        public IReadOnlyList<BaseCharacter> EnemyParty { get; private set; }
        // 탐사 결과 (귀환 후 마을 씬이 읽음)
        public ExpeditionOutcome Outcome { get; private set; }

        public void SetContents(
            IReadOnlyList<BaseCharacter> party,
            ExpeditionInventory supplies,
            DungeonType dungeonType)
        {
            Party = party;
            Supplies = supplies;
            DungeonType = dungeonType;
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
            Party = new List<BaseCharacter>();
            DungeonType = DungeonType.None;
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