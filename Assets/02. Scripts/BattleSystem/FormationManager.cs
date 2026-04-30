using System.Collections.Generic;
using System.Linq;
using _02._Scripts.BattleSystem;
using _02._Scripts.BattleSystem.Interface;
using UnityEngine;
using VContainer;

public enum E_BattleSide
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
        private RuntimeFormationData m_playerData;
        private RuntimeFormationData m_enemyData;
        private IFormationVisualizer m_visualizer;

        [Inject]
        public FormationManager(CharacterSlot playerUnit)
        {
            
        }

        public void SetVisualizer(IFormationVisualizer visualizer)
        {
            m_visualizer = visualizer;
        }

        private RuntimeFormationData GetData(E_BattleSide side) 
            => side == E_BattleSide.Player ? m_playerData : m_enemyData;

        public CharacterSlot GetSlot(BaseCharacter character)
        {
            if (character == null) return null;
            return m_playerData.Slots.Concat(m_enemyData.Slots)
                .FirstOrDefault(s => s != null && s.Occupant == character);
        }

        public FormationMask GetCharacterRank(BaseCharacter character)
        {
            return GetSlot(character)?.rank ?? FormationMask.None;
        }

        public BaseCharacter GetCharacterAt(E_BattleSide side, FormationMask rank)
        {
            return GetData(side).Slots.FirstOrDefault(s => s != null && (s.rank & rank) != 0)?.Occupant;
        }

        public void SwapFormation(BaseCharacter fromCharacter, BaseCharacter toCharacter)
        {
            var fromSlot = GetSlot(fromCharacter);
            var toSlot = GetSlot(toCharacter);

            if (fromSlot == null || toSlot == null || fromSlot.side != toSlot.side)
                return;

            // 로직 시스템이 데이터의 상태를 직접 제어
            fromSlot.SetOccupant(toCharacter);
            toSlot.SetOccupant(fromCharacter);

            m_visualizer?.PlaySwapEffect(fromCharacter, toCharacter);
        }

        public void MoveCharacter(BaseCharacter character, E_BattleSide side, int targetIndex)
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

                m_visualizer?.PlayMoveEffect(character, targetSlot.rank);
            }
        }

        public bool IsSkillUsable(BaseCharacter character, FormationMask skillUsableMask)
        {
            var slot = GetSlot(character);
            return slot != null && (slot.rank & skillUsableMask) != 0;
        }

        public bool IsTargetable(BaseCharacter target, FormationMask targetMask)
        {
            var slot = GetSlot(target);
            return slot != null && (slot.rank & targetMask) != 0;
        }

        public void ConsolidationFormation(E_BattleSide side)
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

            m_visualizer?.PlayConsolidationEffect(side);
        }

        public bool HasAnyValidTarget(BaseCharacter caster, SkillData skillData)
        {
            var enemySide = (caster.CurrentSlot.side == E_BattleSide.Player)? E_BattleSide.Enemy :  E_BattleSide.Player;

            for (int i = 0; i < 4; i++)
            {
                FormationMask rankToCheck = (FormationMask)(1 << i);
                if ((skillData.EnemyTargetMask & (int)rankToCheck) != 0)
                {
                    if (GetCharacterAt(enemySide, rankToCheck) != null) return true;
                }
            }

            return false;
        }

        #region Helper Methods
        public CharacterSlot GetSlotAt(E_BattleSide side, FormationMask rank)
        {
            return GetData(side).Slots.FirstOrDefault(s => s != null && s.rank == rank);
        }

        public void SetCharacterToSlot(BaseCharacter character, E_BattleSide side, int index)
        {
            if (index < 0 || index >= 4) return;
            GetData(side).Slots[index]?.SetOccupant(character);
        }
        #endregion
    }
}
