using System.Collections.Generic;
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
    // 타입별 목록 — 생성자에서 미리 구성해 조회 시 할당 없음
    private readonly Dictionary<SkillType, List<SkillData>> _skillsByType;
    // 전체 목록 캐시
    private readonly List<SkillData> _allSkillsList;
    // 빈 목록 — 타입 미존재 시 반환용 (할당 없음)
    private static readonly IReadOnlyList<SkillData> _emptyList = new List<SkillData>(0);

    // 캐릭터 → (skillId → 남은 쿨타임)
    private readonly Dictionary<BaseCharacter, Dictionary<string, int>> _coolTimeTracker;
    // TickCoolTimes 내부에서 키를 임시 보관하는 재사용 버퍼
    private readonly List<string> _keyBuffer = new List<string>();

    // ── 생성자: VContainer가 SkillData[] 주입 ──────
    public SkillManager(SkillData[] allSkills)
    {
        _skillDict       = new Dictionary<string, SkillData>(allSkills.Length);
        _skillsByType    = new Dictionary<SkillType, List<SkillData>>();
        _allSkillsList   = new List<SkillData>(allSkills.Length);
        _coolTimeTracker = new Dictionary<BaseCharacter, Dictionary<string, int>>();

        foreach (SkillData skill in allSkills)
        {
            if (string.IsNullOrEmpty(skill.Id))
            {
                Debug.LogWarning($"[SkillManager] SkillId가 비어있는 에셋 발견: {skill.name}");
                continue;
            }

            _skillDict[skill.Id] = skill;
            _allSkillsList.Add(skill);

            // 타입별 목록 미리 구성
            if (_skillsByType.ContainsKey(skill.Type) == false)
                _skillsByType[skill.Type] = new List<SkillData>();
            _skillsByType[skill.Type].Add(skill);
        }

        Debug.Log($"[SkillManager] 초기화 완료 — 등록된 스킬 수: {_skillDict.Count}");
    }

    // ── ISkillManager 구현 ────────────────────────

    public SkillData GetSkill(string skillId)
    {
        _skillDict.TryGetValue(skillId, out SkillData skill);
        if (skill == null)
            Debug.LogWarning($"[SkillManager] 스킬을 찾을 수 없음: {skillId}");
        return skill;
    }

    // 캐시된 리스트 반환 — 할당 없음
    public IReadOnlyList<SkillData> GetSkillsByType(SkillType type)
    {
        if (_skillsByType.TryGetValue(type, out List<SkillData> list))
            return list;
        return _emptyList;
    }

    // 캐시된 리스트 반환 — 할당 없음
    public IReadOnlyList<SkillData> GetAllSkills() => _allSkillsList;

    // 쿨타임 등록
    public void RegisterCoolTime(BaseCharacter character, string skillId, int coolTime)
    {
        if (_coolTimeTracker.ContainsKey(character) == false)
            _coolTimeTracker[character] = new Dictionary<string, int>();

        _coolTimeTracker[character][skillId] = coolTime;
        Debug.Log($"[SkillManager] 쿨타임 등록: {character.Name} / {skillId} = {coolTime}턴");
    }

    // 턴마다 쿨타임 감소 — _keyBuffer 재사용으로 할당 없음
    public void TickCoolTimes(BaseCharacter character)
    {
        if (_coolTimeTracker.TryGetValue(character, out Dictionary<string, int> coolTimes) == false) return;

        _keyBuffer.Clear();
        foreach (string key in coolTimes.Keys)
            _keyBuffer.Add(key);

        foreach (string key in _keyBuffer)
        {
            if (coolTimes[key] > 0)
                coolTimes[key]--;
        }
    }

    // 남은 쿨타임 반환 로직
    public int GetRemainingCoolTime(BaseCharacter character, string skillId)
    {
        if (_coolTimeTracker.TryGetValue(character, out Dictionary<string, int> coolTimes))
            if (coolTimes.TryGetValue(skillId, out int remaining))
                return remaining;
        return 0;
    }
}
