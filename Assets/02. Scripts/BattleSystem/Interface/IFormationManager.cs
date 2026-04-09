using _02._Scripts.BattleSystem;
using _03._PipeLine;

namespace _02._Scripts.BattleSystem.Interface
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
        BaseCharacter GetCharacterAt(e_BattleSide side, FormationMask rank);

        /// <summary>
        /// 두 캐릭터의 위치를 교체합니다.
        /// </summary>
        void SwapFormation(BaseCharacter fromCharacter, BaseCharacter toCharacter);

        /// <summary>
        /// 특정 캐릭터를 지정된 위치로 이동시킵니다.
        /// </summary>
        void MoveCharacter(BaseCharacter character, e_BattleSide side, int targetIndex);

        /// <summary>
        /// 캐릭터가 현재 위치에서 해당 스킬을 사용할 수 있는지 확인합니다.
        /// </summary>
        bool IsSkillUsable(BaseCharacter character, FormationMask skillUsableMask);

        /// <summary>
        /// 대상이 해당 스킬의 타겟 범위 내에 있는지 확인합니다.
        /// </summary>
        bool IsTargetable(BaseCharacter target, FormationMask targetMask);

        /// <summary>
        /// 진영 내 빈 공간을 메우기 위해 캐릭터들을 앞으로 당깁니다. (다키스트 던전 스타일)
        /// </summary>
        void ConsolidationFormation(e_BattleSide side);
    }
}
