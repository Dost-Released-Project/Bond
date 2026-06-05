using System.Collections.Generic;

namespace Reactions
{
    public enum SealKind
    {
        All,           // 모든 리액션 봉인
        DesignedOnly,  // 역할(설계) 리액션만 = RoleReactions
        Slots          // fire 시점에 확정된 특정 슬롯들
    }

    /// <summary>
    /// 캐릭터의 리액션을 일정 턴 동안 봉인하는 상태 1개.
    /// 지속은 "봉인 받은 캐릭터의 자기 턴" 수로 카운트한다(버프와 동일 tick).
    /// 봉인 여부는 BaseCharacter.IsReactionSealed 가 단일 기준으로 판정 — Resolve(발화 후보 제외)와
    /// (추후) 실행 직전 재확인 양쪽에서 같은 메서드를 쓴다.
    /// </summary>
    public class ReactionSeal
    {
        public SealKind Kind;
        public HashSet<Reaction> Slots;   // Kind==Slots 일 때만 사용
        public int RemainingTurns;
    }
}
