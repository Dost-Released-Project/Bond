using UnityEngine;

namespace _02._Scripts.BattleSystem_KWT
{
    /// <summary>
    /// [L] 전투 로직 담당 (Pure C#)
    /// </summary>
    public class BattleManager
    {
        /// <summary>
        /// 이제 행동 순서 판단은 TurnManager가 담당하므로, 
        /// 여기서는 순수하게 전투 결과(데미지 등) 계산만 수행합니다.
        /// </summary>
        public int GetTestDamage(int attackerId, int str, int targetId, int def)
        {
            // 기획서 공식 대신 연관 스탯 로그 출력
            Debug.Log($"<color=cyan>[BattleManager]</color> 데미지 계산 요청 - 공격자({attackerId}) STR: {str} VS 방어자({targetId}) DEF: {def}");
            
            // 테스트용 고정 데미지 반환
            return 10;
        }

        // TODO: 기획서 기반의 4번 항목(데미지 계산), 3번 항목(스탯 보정) 등 전투 규칙 로직이 여기에 위치하게 됩니다.
    }
}
