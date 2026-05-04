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

    public class ExpeditionPayload
    {
        // 마을 씬 → 탐사 씬으로 넘기는 데이터 컨테이너
        // 에셋이라 씬 전환 후에도 유지됨
        // OnEnable에서 초기화하여 플레이 시작마다 찌꺼기 제거

        public IReadOnlyList<BaseCharacter> Party { get; private set; }
        public ExpeditionInventory Supplies { get; private set; }
        public string DungeonId { get; private set; }

        // 탐사 결과 (귀환 후 마을 씬이 읽음)
        public ExpeditionOutcome Outcome { get; private set; }

        private void OnEnable()
        {
            Clear();
        }

        public void SetContents(
            IReadOnlyList<BaseCharacter> party,
            ExpeditionInventory supplies,
            string dungeonId)
        {
            Party = party;
            Supplies = supplies;
            DungeonId = dungeonId;
        }

        public void SetResult(ExpeditionOutcome outcome)
        {
            Outcome = outcome;
        }

        public void Clear()
        {
            Party = new List<BaseCharacter>();
            DungeonId = string.Empty;
            Outcome = ExpeditionOutcome.None;
        }

        public override string ToString()
        {
            string str = "";
            foreach (var c in Party)
            {
                str += $"{c.Data.Name}\n";
            }

            foreach (var slot in Supplies.GetAll())
            {
                str += $"{slot.item.itemName}\n";
            }
            return str;
        }
    }
}