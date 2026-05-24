using BattleSystem;
using PipeLine;

namespace BattleSystem.Interface
{
    /// <summary>
    /// 진영 위치 및 스킬 사용 조건(비트마스크)을 관리하는 인터페이스
    /// </summary>
    public interface IFormationManager
    {
        /// <summary>
        /// 특정 캐릭터의 현재 진영 위치(Rank)를 반환합니다.
        /// </summary>
        FormationMask GetCharacterRank(BaseCharacter character);

        /// <summary>
        /// 특정 진영의 특정 위치(Rank)에 있는 캐릭터를 반환합니다.
        /// </summary>
        BaseCharacter GetCharacterAt(E_BattleSide side, FormationMask rank);

        /// <summary>
        /// 두 캐릭터의 위치를 교체합니다.
        /// </summary>
        void SwapFormation(BaseCharacter fromCharacter, BaseCharacter toCharacter);

        /// <summary>
        /// 특정 캐릭터를 지정된 위치로 이동시킵니다.
        /// </summary>
        void MoveCharacter(BaseCharacter character, E_BattleSide side, int targetIndex);

        /// <summary>
        /// 캐릭터가 현재 위치에서 해당 스킬을 사용할 수 있는지 확인합니다.
        /// </summary>
        bool HasAnyValidTarget(BaseCharacter character, SkillData skill);
        
        void SetCharacterToSlot(BaseCharacter character, E_BattleSide side, int index);

        /// <summary>
        /// 대상이 해당 스킬의 타겟 범위 내에 있는지 확인합니다.
        /// </summary>
        bool IsTargetable(BaseCharacter target, FormationMask targetMask);

        /// <summary>
        /// 진영 내 빈 공간을 메우기 위해 캐릭터들을 앞으로 당깁니다. (다키스트 던전 스타일)
        /// </summary>
        void ConsolidationFormation(E_BattleSide side);

        /// <summary>
        /// 특정 캐릭터를 진영 슬롯에서 비웁니다. (사망 시 호출)
        /// </summary>
        void ClearCharacter(BaseCharacter character);

        /// <summary>
        /// 시전자의 진영에 맞춰 올바르게 반전된 사용 가능 위치(UseableSlots) 마스크를 반환합니다.
        /// </summary>
        int GetUseableMask(BaseCharacter caster, SkillData skill);

        /// <summary>
        /// 시전자의 진영에 맞춰 올바르게 반전된 타겟 적용 범위(TargetMask) 마스크를 반환합니다.
        /// </summary>
        int GetTargetMask(BaseCharacter caster, SkillData skill);
    }
}
