using System.Collections.Generic;
using System.Linq;
using _02._Scripts.BattleSystem;
using _02._Scripts.BattleSystem.Interface;

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
    public FormationMask Rank { get; }
    public BaseCharacter Occupant { get; private set; }

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
    /// <summary>
    /// [L] Logic (System): 진영 배치를 판단하고 제어합니다.
    /// 직접적인 데이터 수정은 RuntimeData에게, 연출은 Visualizer에게 위임합니다.
    /// </summary>
    public class FormationManager : IFormationManager
    {
        private readonly RuntimeDataFormation _runtimeData;
        private IFormationVisualizer _visualizer;

        public FormationManager(RuntimeDataFormation runtimeData)
        {
            _runtimeData = runtimeData;
        }

        public void SetVisualizer(IFormationVisualizer visualizer)
        {
            _visualizer = visualizer;
        }

        public BattleSlot GetSlot(BaseCharacter character) => _runtimeData.GetSlot(character);

        public FormationMask GetCharacterRank(BaseCharacter character)
        {
            var slot = GetSlot(character);
            return slot?.Rank ?? FormationMask.None;
        }

        public BaseCharacter GetCharacterAt(e_BattleSide side, FormationMask rank)
        {
            return _runtimeData.GetSlots(side).FirstOrDefault(s => (s.Rank & rank) != 0)?.Occupant;
        }

        public void SwapFormation(BaseCharacter fromCharacter, BaseCharacter toCharacter)
        {
            var fromSlot = _runtimeData.GetSlot(fromCharacter);
            var toSlot = _runtimeData.GetSlot(toCharacter);

            if (fromSlot == null || toSlot == null || fromSlot.Side != toSlot.Side)
                return;

            // [D] 데이터 갱신 요청
            _runtimeData.SwapOccupants(fromSlot, toSlot);

            // [V] 비주얼 연출 명령
            _visualizer?.PlaySwapEffect(fromCharacter, toCharacter);
        }

        public void MoveCharacter(BaseCharacter character, e_BattleSide side, int targetIndex)
        {
            if (targetIndex < 0 || targetIndex >= 4) return;

            var currentSlot = _runtimeData.GetSlot(character);
            var sideSlots = _runtimeData.GetSlots(side);
            var targetSlot = sideSlots[targetIndex];

            if (currentSlot == null) return;

            if (!targetSlot.IsEmpty)
            {
                SwapFormation(character, targetSlot.Occupant);
            }
            else
            {
                // [D] 데이터 갱신 요청
                int currentIndex = System.Array.IndexOf(_runtimeData.GetSlots(currentSlot.Side), currentSlot);
                _runtimeData.ClearSlot(currentSlot.Side, currentIndex);
                _runtimeData.UpdateSlot(side, targetIndex, character);

                // [V] 비주얼 연출 명령
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
            var sideSlots = _runtimeData.GetSlots(side);
            List<BaseCharacter> characters = sideSlots
                .Where(s => !s.IsEmpty)
                .Select(s => s.Occupant)
                .ToList();

            // [D] 데이터 순차적 갱신
            for (int i = 0; i < sideSlots.Length; i++)
            {
                if (i < characters.Count)
                    _runtimeData.UpdateSlot(side, i, characters[i]);
                else
                    _runtimeData.ClearSlot(side, i);
            }

            // [V] 비주얼 연출 명령
            _visualizer?.PlayConsolidationEffect(side);
        }

        #region Helper Methods
        public BattleSlot GetSlotAt(e_BattleSide side, FormationMask rank)
        {
            return _runtimeData.GetSlots(side).FirstOrDefault(s => s.Rank == rank);
        }

        public void SetCharacterToSlot(BaseCharacter character, e_BattleSide side, int index)
        {
            if (index < 0 || index >= 4) return;
            _runtimeData.UpdateSlot(side, index, character);
        }
        #endregion
    }
}
