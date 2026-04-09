using System.Collections.Generic;
using System.Linq;
using _02._Scripts.BattleSystem;
using _02._Scripts.BattleSystem.Interface;
using UnityEngine;
using VContainer;

public enum e_BattleSide
{
    Player,
    Enemy
}

namespace _02._Scripts.BattleSystem
{
    /// <summary>
    /// [L] Logic (System): 진영 배치를 판단하고 제어하는 유일한 중심점입니다.
    /// RuntimeDataFormation(Data)의 상태를 직접 수정하며 Visualizer(Visual)에 명령을 내립니다.
    /// </summary>
    public class FormationManager : IFormationManager
    {
        private RuntimeFormationData _playerData;
        private RuntimeFormationData _enemyData;
        private IFormationVisualizer _visualizer;

        [Inject]
        public FormationManager(CharacterSlot playerUnit)
        {
            
        }

        public void SetVisualizer(IFormationVisualizer visualizer)
        {
            _visualizer = visualizer;
        }

        private RuntimeFormationData GetData(e_BattleSide side) 
            => side == e_BattleSide.Player ? _playerData : _enemyData;

        public CharacterSlot GetSlot(BaseCharacter character)
        {
            if (character == null) return null;
            return _playerData.Slots.Concat(_enemyData.Slots)
                .FirstOrDefault(s => s != null && s.Occupant == character);
        }

        public FormationMask GetCharacterRank(BaseCharacter character)
        {
            return GetSlot(character)?.Rank ?? FormationMask.None;
        }

        public BaseCharacter GetCharacterAt(e_BattleSide side, FormationMask rank)
        {
            return GetData(side).Slots.FirstOrDefault(s => s != null && (s.Rank & rank) != 0)?.Occupant;
        }

        public void SwapFormation(BaseCharacter fromCharacter, BaseCharacter toCharacter)
        {
            var fromSlot = GetSlot(fromCharacter);
            var toSlot = GetSlot(toCharacter);

            if (fromSlot == null || toSlot == null || fromSlot.Side != toSlot.Side)
                return;

            // 로직 시스템이 데이터의 상태를 직접 제어
            fromSlot.SetOccupant(toCharacter);
            toSlot.SetOccupant(fromCharacter);

            _visualizer?.PlaySwapEffect(fromCharacter, toCharacter);
        }

        public void MoveCharacter(BaseCharacter character, e_BattleSide side, int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= 4) return;

            var currentSlot = GetSlot(character);
            var targetData = GetData(side);
            var targetSlot = targetData.Slots[targetIndex];

            if (currentSlot == null || targetSlot == null) return;

            if (!targetSlot.IsEmpty)
            {
                SwapFormation(character, targetSlot.Occupant);
            }
            else
            {
                currentSlot.Clear();
                targetSlot.SetOccupant(character);

                _visualizer?.PlayMoveEffect(character, targetSlot.Rank);
            }
        }

        public bool IsSkillUsable(BaseCharacter character, FormationMask skillUsableMask)
        {
            var slot = GetSlot(character);
            return slot != null && (slot.Rank & skillUsableMask) != 0;
        }

        public bool IsTargetable(BaseCharacter target, FormationMask targetMask)
        {
            var slot = GetSlot(target);
            return slot != null && (slot.Rank & targetMask) != 0;
        }

        public void ConsolidationFormation(e_BattleSide side)
        {
            var data = GetData(side);
            var slots = data.Slots;
            
            var characters = slots
                .Where(s => s != null && !s.IsEmpty)
                .Select(s => s.Occupant)
                .ToList();

            for (int i = 0; i < slots.Length; i++)
            {
                if (slots[i] == null) continue;
                
                if (i < characters.Count)
                    slots[i].SetOccupant(characters[i]);
                else
                    slots[i].Clear();
            }

            _visualizer?.PlayConsolidationEffect(side);
        }

        #region Helper Methods
        public CharacterSlot GetSlotAt(e_BattleSide side, FormationMask rank)
        {
            return GetData(side).Slots.FirstOrDefault(s => s != null && s.Rank == rank);
        }

        public void SetCharacterToSlot(BaseCharacter character, e_BattleSide side, int index)
        {
            if (index < 0 || index >= 4) return;
            GetData(side).Slots[index]?.SetOccupant(character);
        }
        #endregion
    }
}
