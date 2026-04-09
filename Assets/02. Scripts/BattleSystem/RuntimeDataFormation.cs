using System;
using System.Collections.Generic;
using System.Linq;
using _02._Scripts.BattleSystem;

namespace _02._Scripts.BattleSystem
{
    /// <summary>
    /// [D] Runtime Data: 진영의 실시간 상태를 관리합니다.
    /// 모든 데이터 변경은 이 클래스를 통해 이루어지며 이벤트를 발송합니다.
    /// </summary>
    public class RuntimeDataFormation
    {
        private Dictionary<e_BattleSide, BattleSlot[]> _Slots;

        // 데이터 변경 시 발생하는 이벤트 (Observable)
        public event Action<e_BattleSide, int, BaseCharacter> OnSlotChanged;
        public event Action<BaseCharacter, BaseCharacter> OnSlotsSwapped;

        public RuntimeDataFormation()
        {
            _Slots = new Dictionary<e_BattleSide, BattleSlot[]>
            {
                { e_BattleSide.Player, CreateSlots(e_BattleSide.Player) },
                { e_BattleSide.Enemy, CreateSlots(e_BattleSide.Enemy) }
            };
        }

        private BattleSlot[] CreateSlots(e_BattleSide side)
        {
            BattleSlot[] slots = new BattleSlot[4];
            for (int i = 0; i < 4; i++)
            {
                FormationMask rank = (FormationMask)(1 << i);
                slots[i] = new BattleSlot(side, rank);
            }
            return slots;
        }

        public BattleSlot[] GetSlots(e_BattleSide side) => _Slots[side];

        public BattleSlot GetSlot(BaseCharacter character)
        {
            if (character == null) return null;
            return _Slots.Values.SelectMany(s => s).FirstOrDefault(slot => slot.Occupant == character);
        }

        public void UpdateSlot(e_BattleSide side, int index, BaseCharacter character)
        {
            _Slots[side][index].SetOccupant(character);
            OnSlotChanged?.Invoke(side, index, character);
        }

        public void ClearSlot(e_BattleSide side, int index)
        {
            _Slots[side][index].Clear();
            OnSlotChanged?.Invoke(side, index, null);
        }

        public void SwapOccupants(BattleSlot slotA, BattleSlot slotB)
        {
            var charA = slotA.Occupant;
            var charB = slotB.Occupant;

            slotA.SetOccupant(charB);
            slotB.SetOccupant(charA);

            OnSlotsSwapped?.Invoke(charA, charB);
        }
    }
}
