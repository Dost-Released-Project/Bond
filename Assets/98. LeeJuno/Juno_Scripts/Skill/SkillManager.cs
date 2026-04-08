using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VContainer;

/// <summary>
/// ISkillManager 구현체.
/// SkillData Dictionary 관리 + 캐릭터별 쿨타임 추적을 담당한다.
/// 비트마스크 해석(슬롯 매핑 등)은 담당하지 않는다.
/// VContainer의 SkillScope에서 Singleton으로 등록된다.
/// </summary>
public class SkillManager : ISkillManager
{
    // ── 내부 저장소 ───────────────────────────────
    // ID → SkillData
    private readonly Dictionary<string, SkillData> _skillDict;
    // 캐릭터 → (skillId → 남은 쿨타임)
    private readonly Dictionary<BaseCharacter, Dictionary<string, int>> _coolTimeTracker;

    // ── 생성자: VContainer가 SkillData[] 주입 ──────
    [Inject]
    public SkillManager(SkillData[] allSkills)
    {
        _skillDict = new Dictionary<string, SkillData>(allSkills.Length);
        _coolTimeTracker = new Dictionary<BaseCharacter, Dictionary<string, int>>();

        foreach (SkillData skill in allSkills)
        {
            if (string.IsNullOrEmpty(skill.SkillId))
            {
                Debug.LogWarning($"[SkillManager] SkillId가 비어있는 에셋 발견: {skill.name}");
                continue;
            }

            _skillDict[skill.SkillId] = skill;
        }

        Debug.Log($"[SkillManager] 초기화 완료 — 등록된 스킬 수: {_skillDict.Count}");
    }

    // ── ISkillManager 구현 ────────────────────────

    public SkillData GetSkill(string skillId)
    {
        _skillDict.TryGetValue(skillId, out var skill);
        if (skill == null)
            Debug.LogWarning($"[SkillManager] 스킬을 찾을 수 없음: {skillId}");
        return skill;
    }

    public IReadOnlyList<SkillData> GetSkillsByType(SkillType type)
        => _skillDict.Values.Where(s => s.Type == type).ToList();

    public IReadOnlyList<SkillData> GetAllSkills()
        => _skillDict.Values.ToList();

    // 쿨타임 등록
    public void RegisterCoolTime(BaseCharacter character, string skillId, int coolTime)
    {
        if (_coolTimeTracker.ContainsKey(character) == false)
            _coolTimeTracker[character] = new Dictionary<string, int>();

        _coolTimeTracker[character][skillId] = coolTime;
        Debug.Log($"[SkillManager] 쿨타임 등록: {character.name} / {skillId} = {coolTime}턴");
    }

    // 턴마다 쿨타임 감소 로직
    public void TickCoolTimes(BaseCharacter character)
    {
        if (_coolTimeTracker.TryGetValue(character, out Dictionary<string, int> coolTimes) == false) return;

        foreach (var key in coolTimes.Keys.ToList())
        {
            if (coolTimes[key] > 0)
                coolTimes[key]--;
        }
    }

    public int GetRemainingCoolTime(BaseCharacter character, string skillId)
    {
        if (_coolTimeTracker.TryGetValue(character, out Dictionary<string, int> coolTimes))
            if (coolTimes.TryGetValue(skillId, out int remaining))
                return remaining;
        return 0;
    }
}