using System;
using Newtonsoft.Json;
using Reactions;

/// <summary>
/// CharacterDetail 스킬 그리드용 스킬 편성 API.
/// Skills 배열은 항상 "압축" 상태로 유지한다(빈 슬롯은 꼬리에만 존재).
/// 제거로 슬롯이 당겨지면 리액션의 SkillCastReactionEffect.SkillIndex 를 재매핑해
/// 같은 스킬을 계속 가리키게 하고, 사라진 스킬을 가리키던 행동은 해제(-1)한다.
/// </summary>
public partial class BaseCharacter
{
    /// <summary>스킬 편성(추가/제거/교체) 변경 — 영속 트리거(Roster 자동저장).</summary>
    public event Action<BaseCharacter> OnSkillsChanged;

    /// <summary>비어있지 않은 장착 스킬 개수.</summary>
    [JsonIgnore]
    public int EquippedSkillCount
    {
        get
        {
            int n = 0;
            for (int i = 0; i < Skills.Length; i++)
                if (Skills[i] != null) n++;
            return n;
        }
    }

    /// <summary>data 가 이미 편성돼 있으면 그 슬롯 인덱스, 아니면 -1.</summary>
    public int IndexOfSkill(SkillData data)
    {
        if (data == null) return -1;
        for (int i = 0; i < Skills.Length; i++)
            if (Skills[i]?.Data == data) return i;
        return -1;
    }

    public bool HasSkill(SkillData data) => IndexOfSkill(data) >= 0;

    /// <summary>
    /// 그리드 토글. 이미 편성돼 있으면 제거(압축 + 리액션 재매핑), 아니면 꼬리 슬롯에 추가.
    /// 추가 시 슬롯이 가득(최대 개수) 차 있으면 무시. 실제 변경이 일어나면 true.
    /// </summary>
    public bool ToggleSkill(SkillData data)
    {
        if (data == null) return false;
        int existing = IndexOfSkill(data);
        if (existing >= 0)
        {
            RemoveSkillAt(existing);
            return true;
        }
        return AddSkill(data);
    }

    /// <summary>첫 빈 꼬리 슬롯에 추가. 가득 찼거나 중복이면 false.</summary>
    public bool AddSkill(SkillData data)
    {
        if (data == null || HasSkill(data)) return false;
        int slot = FirstEmptySlot();
        if (slot < 0) return false;            // 가득 참 — 추가 차단(최대 개수)
        Skills[slot] = new SampleSkill(data);
        OnSkillsChanged?.Invoke(this);
        return true;
    }

    /// <summary>
    /// 슬롯의 스킬을 제거하고 뒤 슬롯을 한 칸씩 당긴다(압축). 마지막 슬롯은 비운다.
    /// 제거 슬롯을 가리키던 리액션 행동은 해제, 뒤를 가리키던 인덱스는 1 감소.
    /// </summary>
    public void RemoveSkillAt(int index)
    {
        if (index < 0 || index >= Skills.Length || Skills[index] == null) return;

        for (int i = index; i < Skills.Length - 1; i++)
            Skills[i] = Skills[i + 1];
        Skills[Skills.Length - 1] = null;

        RemapReactionSkillIndices(index);
        OnSkillsChanged?.Invoke(this);
        RaiseReactionsChanged();              // 리액션 내부(SkillIndex) 제자리 수정 — 영속/표시 트리거
    }

    private int FirstEmptySlot()
    {
        for (int i = 0; i < Skills.Length; i++)
            if (Skills[i] == null) return i;
        return -1;
    }

    /// <summary>
    /// 슬롯 removedIndex 제거(압축)에 맞춰 모든 리액션의 SkillCastReactionEffect.SkillIndex 보정.
    /// == removedIndex → -1(해제), > removedIndex → 1 감소, 그 외 유지.
    /// </summary>
    private void RemapReactionSkillIndices(int removedIndex)
    {
        Remap(RoleReactions);
        Remap(TraitReactions);

        void Remap(Reaction[] reactions)
        {
            if (reactions == null) return;
            foreach (var r in reactions)
            {
                if (r?.BaseEffect is SkillCastReactionEffect cast)
                {
                    if (cast.SkillIndex == removedIndex) cast.SkillIndex = -1;
                    else if (cast.SkillIndex > removedIndex) cast.SkillIndex -= 1;
                }
            }
        }
    }
}