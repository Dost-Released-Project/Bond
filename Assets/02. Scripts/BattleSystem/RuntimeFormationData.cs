using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BattleSystem
{
    /// <summary>
    /// [D] Runtime Data: 특정 진영의 슬롯 참조 데이터만 관리합니다.
    /// 모든 데이터의 물리적인 변경과 결정은 FormationManager(Logic)에서 수행합니다.
    /// </summary>
    public class RuntimeFormationData
    {
        public E_BattleSide Side { get; }
        public readonly CharacterSlot[] Slots = new CharacterSlot[4];

        public RuntimeFormationData(E_BattleSide side, IEnumerable<CharacterSlot> sceneSlots)
        {
            Side = side;
            
            if (sceneSlots == null) return;

            // 해당 진영의 슬롯만 필터링하여 Rank 순서대로 등록
            var sideSlots = sceneSlots
                .Where(s => s != null && s.side == side)
                .OrderBy(s => (int)s.rank)
                .ToArray();

            for (int i = 0; i < sideSlots.Length && i < 4; i++)
            {
                Slots[i] = sideSlots[i];
            }
        }
    }
}
