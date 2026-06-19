using System.Collections.Generic;
using BattleSystem;

// BaseCharacter 의 진영 이동 편의 메서드. 자신이 보유한 FormationManager 로 이동을 수행한다.
// (m_formationManager 는 다른 partial 의 private 필드지만 같은 클래스라 접근 가능)
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

    /// <summary>
    /// 해당 스킬의 유효 타겟(진영 + 사거리 마스크)에 target 이 포함되는지.
    /// 전투 시스템과 동일하게 FormationManager.GetValidSlots 로 판정한다(EnemyTargetMask/AllyTargetMask 반영).
    /// 단, 시전 위치(UseableSlots) 가용성은 별도 — 이 메서드는 "타겟이 사거리에 드는지"만 본다.
    /// </summary>
    public bool CanSkillTarget(SkillData skill, BaseCharacter target)
    {
        if (m_formationManager == null || skill == null || target == null) return false;
        foreach (var slot in m_formationManager.GetValidSlots(this, skill))
            if (slot != null && !slot.IsEmpty && slot.Occupant == target) return true;
        return false;
    }

    /// <summary>
    /// 시전자가 실제로 "선택 가능한" 타겟 슬롯 = 기하 유효 슬롯(FormationManager.GetValidSlots) ∩ 행동 허용.
    /// 보호/보조(DEFENSIVE/SUPPORT) 스킬은 불협조(IsUncooperativeWith) 중인 아군을 제외한다(TRT_004 등).
    /// FormationManager 는 순수 기하/마스크 판정만 담당하고, 관계·행동 제약은 여기(시전자)에서 합성한다.
    /// 수동 UI(BattleFormationPresenter)·자동 AI·스킬 가용성(HasSelectableTarget)이 공통으로 쓴다.
    /// </summary>
    public List<CharacterSlot> GetSelectableSlots(SkillData skill)
    {
        var slots = m_formationManager != null
            ? m_formationManager.GetValidSlots(this, skill)
            : new List<CharacterSlot>();

        if (skill != null && (skill.Type == SkillType.DEFENSIVE || skill.Type == SkillType.SUPPORT))
            slots.RemoveAll(s => s != null && !s.IsEmpty && IsUncooperativeWith(s.Occupant));

        return slots;
    }

    /// <summary>GetSelectableSlots 에 점유 중인 타겟이 하나라도 있는지(스킬 사용 가능 판정용).</summary>
    public bool HasSelectableTarget(SkillData skill)
    {
        foreach (var s in GetSelectableSlots(skill))
            if (s != null && !s.IsEmpty) return true;
        return false;
    }
}
