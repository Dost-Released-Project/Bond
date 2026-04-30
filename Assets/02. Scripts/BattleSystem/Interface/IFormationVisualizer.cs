using BattleSystem;

namespace BattleSystem.Interface
{
    /// <summary>
    /// [V] Visual Interface: Logic이 Visual에게 내리는 명령 명세서입니다.
    /// </summary>
    public interface IFormationVisualizer
    {
        /// <summary>
        /// 캐릭터가 특정 슬롯으로 이동하는 연출을 수행합니다.
        /// </summary>
        void PlayMoveEffect(BaseCharacter character, FormationMask targetRank);

        /// <summary>
        /// 두 캐릭터의 위치가 바뀌는 연출을 수행합니다.
        /// </summary>
        void PlaySwapEffect(BaseCharacter fromCharacter, BaseCharacter toCharacter);

        /// <summary>
        /// 진영이 재정렬될 때의 연출을 수행합니다.
        /// </summary>
        void PlayConsolidationEffect(E_BattleSide side);
    }
}
