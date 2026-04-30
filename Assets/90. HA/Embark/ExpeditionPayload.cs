using System.Collections.Generic;
using _02._Scripts.BattleSystem;
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

        public IReadOnlyList<BaseCharacterData> Party { get; private set; }
        public IInventory Supplies { get; private set; }
        public string DungeonId { get; private set; }

        // 탐사 결과 (귀환 후 마을 씬이 읽음)
        public ExpeditionOutcome Outcome { get; private set; }

        private void OnEnable()
        {
            Clear();
        }

        public void SetContents(
            IReadOnlyList<BaseCharacterData> party,
            IInventory supplies,
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
            Party = new List<BaseCharacterData>();
            DungeonId = string.Empty;
            Outcome = ExpeditionOutcome.None;
        }
    }
}