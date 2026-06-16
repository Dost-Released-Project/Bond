using System.Collections.Generic;
using Reactions;
using UnityEngine;

// BaseCharacter 의 리액션 봉인 파트. 버프와 동일하게 전투 런타임 한정 상태이며
// 만료 tick(TickSeals)도 자기 턴 시작에 버프와 같은 타이밍으로 돈다.
public partial class BaseCharacter
{
    private readonly List<ReactionSeal> _seals = new List<ReactionSeal>();

    public IReadOnlyList<ReactionSeal> ActiveSeals => _seals;

    public void ApplySeal(ReactionSeal seal)
    {
        if (seal == null) return;
        _seals.Add(seal);
        OnBuffsChanged?.Invoke(this); // 전투 상태 변경 공용 신호(버프/봉인) — UI 갱신용
        Debug.Log($"<color=orange>[봉인]</color> {Name} 리액션 봉인 ({seal.Kind}, {seal.RemainingTurns}턴)");
    }

    /// <summary>
    /// 해당 리액션이 현재 봉인 상태인지. Resolve 의 발화 후보 제외와 (추후) 실행 직전 재확인에서
    /// 같은 기준으로 사용한다.
    /// </summary>
    public bool IsReactionSealed(Reaction reaction)
    {
        if (reaction == null) return false;
        foreach (var s in _seals)
        {
            switch (s.Kind)
            {
                case SealKind.All:
                    return true;
                case SealKind.DesignedOnly:
                    if (System.Array.IndexOf(RoleReactions, reaction) >= 0) return true;
                    break;
                case SealKind.Slots:
                    if (s.Slots != null && s.Slots.Contains(reaction)) return true;
                    break;
            }
        }
        return false;
    }

    /// <summary>자기 턴 시작마다 호출. 봉인 지속을 1 감소시키고 만료분을 제거한다(TickBuffs 와 동일 타이밍).</summary>
    public void TickSeals()
    {
        if (_seals.Count == 0) return;
        bool changed = false;
        for (int i = _seals.Count - 1; i >= 0; i--)
        {
            if (--_seals[i].RemainingTurns <= 0)
            {
                _seals.RemoveAt(i);
                changed = true;
            }
        }
        if (changed) OnBuffsChanged?.Invoke(this);
    }
}
