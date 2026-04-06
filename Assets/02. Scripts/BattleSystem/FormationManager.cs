using System.Collections.Generic;
using System.Linq;
using _02._Scripts.BattleSystem;
using _02._Scripts.BattleSystem.Interface;
using _03._PipeLine;

public enum e_BattleSide
{
    Player,
    Enemy
}

/// <summary>
/// 전투 내의 특정 위치(Slot) 정보를 담는 객체입니다.
/// </summary>
public class BattleSlot
{
    public e_BattleSide Side { get; }
    public FormationMask Rank { get; } // 비트마스크 형태의 위치 정보 (Form1, Form2 등)
    public BaseCharacter Occupant { get; private set; } // 해당 위치에 있는 캐릭터

    public bool IsEmpty => Occupant == null;

    public BattleSlot(e_BattleSide side, FormationMask rank)
    {
        Side = side;
        Rank = rank;
    }

    public void SetOccupant(BaseCharacter character)
    {
        Occupant = character;
    }

    public void Clear()
    {
        Occupant = null;
    }
}

namespace _02._Scripts.BattleSystem
{
    public class FormationManager : IFormationManager
    {
        // 이제 캐릭터 배열이 아닌 슬롯 배열을 관리합니다.
        private Dictionary<e_BattleSide, BattleSlot[]> _Slots;

        public FormationManager()
        {
            _Slots = new Dictionary<e_BattleSide, BattleSlot[]>
            {
                { e_BattleSide.Player, CreateSlots(e_BattleSide.Player) },
                { e_BattleSide.Enemy, CreateSlots(e_BattleSide.Enemy) }
            } ;
        }

        private BattleSlot[] CreateSlots(e_BattleSide side)
        {
            BattleSlot[] slots = new BattleSlot[4];
            for (int i = 0; i < 4; i++)
            {
                // 인덱스(0~3)를 FormationMask(1, 2, 4, 8)로 변환하여 할당
                FormationMask rank = (FormationMask)(1 << i);
                slots[i] = new BattleSlot(side, rank);
            }
            return slots;
        }


        
        public BattleSlot GetSlot(BaseCharacter character)
        {
            if (character == null) return null;

            foreach (var sideSlots in _Slots.Values)
            {
                var slot = sideSlots.FirstOrDefault(s => s.Occupant == character);
                if (slot != null) return slot;
            }
            return null;
        }

        public FormationMask GetCharacterRank(e_BattleSide battleSide, BaseCharacter character)
        {
            var slot = GetSlot(character);
            return slot?.Rank ?? FormationMask.None;
        }
        
        public void SwapFormation(BaseCharacter fromCharacter, BaseCharacter toCharacter)
        {
            var fromSlot = GetSlot(fromCharacter);
            var toSlot = GetSlot(toCharacter);
            
            if (fromSlot == null || toSlot == null || fromSlot.Side != toSlot.Side) 
                return;
            
            // 슬롯의 내용물(Occupant)만 교체합니다.
            BaseCharacter temp = fromSlot.Occupant;
            fromSlot.SetOccupant(toSlot.Occupant);
            toSlot.SetOccupant(temp);
            
            // Visual 로직 들어갈 자리
        }

        public bool IsSkillUsable(BaseCharacter character, FormationMask skillUsableMask)
        {
            var slot = GetSlot(character);
            if (slot == null) return false;

            return (slot.Rank & skillUsableMask) != 0;
        }

        public bool IsTargetable(BaseCharacter target, FormationMask targetMask)
        {
            var slot = GetSlot(target);
            if (slot == null) return false;

            return (slot.Rank & targetMask) != 0;
        }


        #region Helper Methods
        /// <summary>
        /// 특정 진영의 특정 위치(Rank)에 있는 슬롯을 가져옵니다.
        /// </summary>
        public BattleSlot GetSlotAt(e_BattleSide side, FormationMask rank)
        {
            return _Slots[side].FirstOrDefault(s => s.Rank == rank);
        }

        /// <summary>
        /// 캐릭터를 특정 슬롯에 배치합니다 (초기 세팅용).
        /// </summary>
        public void SetCharacterToSlot(BaseCharacter character, e_BattleSide side, int index)
        {
            if (index < 0 || index >= 4) return;
            _Slots[side][index].SetOccupant(character);
        }
        #endregion
    }
}
