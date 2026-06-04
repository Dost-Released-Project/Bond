using System.Collections.Generic;
using BattleSystem;

/// <summary>
/// BaseCharacter 의 진영 이동 편의 메서드. 자신이 보유한 FormationManager 로 이동을 수행한다.
/// (m_formationManager 는 다른 partial 의 private 필드지만 같은 클래스라 접근 가능)
/// </summary>
public partial class BaseCharacter
{
    /// <summary>자기 진영의 index(0=최전방 ~ 3=최후방) 슬롯으로 이동. 대상 슬롯이 차 있으면 교체(Swap)된다.</summary>
    public void MoveToFormationIndex(int index)
    {
        if (m_formationManager == null || CurrentSlot == null) return;
        m_formationManager.MoveCharacter(this, CurrentSlot.side, index);
    }

    /// <summary>상대 진영에서 가장 가까운(frontmost=Rank1쪽) 또는 가장 먼(Rank4쪽) 생존 캐릭터를 반환. 없으면 null.</summary>
    public BaseCharacter GetOpposingByRank(bool frontmost)
    {
        if (m_formationManager == null || CurrentSlot == null) return null;

        var enemySide = CurrentSlot.side == E_BattleSide.Player ? E_BattleSide.Enemy : E_BattleSide.Player;
        FormationMask[] order = frontmost
            ? new[] { FormationMask.Rank1, FormationMask.Rank2, FormationMask.Rank3, FormationMask.Rank4 }
            : new[] { FormationMask.Rank4, FormationMask.Rank3, FormationMask.Rank2, FormationMask.Rank1 };

        foreach (var r in order)
        {
            var c = m_formationManager.GetCharacterAt(enemySide, r);
            if (c != null && !c.IsDead) return c;
        }
        return null;
    }

    /// <summary>같은 진영의 생존 캐릭터를 열거(기본 자신 포함). FormationManager 가 없으면 빈 열거.</summary>
    public IEnumerable<BaseCharacter> GetSameSideAllies(bool includeSelf = true)
    {
        if (m_formationManager == null || CurrentSlot == null) yield break;

        var side = CurrentSlot.side;
        FormationMask[] ranks = { FormationMask.Rank1, FormationMask.Rank2, FormationMask.Rank3, FormationMask.Rank4 };
        foreach (var r in ranks)
        {
            var c = m_formationManager.GetCharacterAt(side, r);
            if (c == null || c.IsDead) continue;
            if (!includeSelf && c == this) continue;
            yield return c;
        }
    }
}
