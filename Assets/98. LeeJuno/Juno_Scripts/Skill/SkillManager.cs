using System.Collections.Generic;
using UnityEngine;
using VContainer;

public class SkillManager : ISkillManager
{
    // ── 내부 저장소 ───────────────────────────────
    // ID → SkillData
    private readonly Dictionary<int, SkillData> _skillDict;
    // 타입별 목록 — 생성자에서 미리 구성해 조회 시 할당 없음
    private readonly Dictionary<SkillType, List<SkillData>> _skillsByType;
    // 전체 목록 캐시
    private readonly List<SkillData> _allSkillsList;
    // 빈 목록 — 타입 미존재 시 반환용 (할당 없음)
    private static readonly IReadOnlyList<SkillData> _emptyList = new List<SkillData>(0);

    // 캐릭터 → (skillId(int) → 남은 쿨타임)
    private readonly Dictionary<BaseCharacter, Dictionary<int, int>> _coolTimeTracker;
    
    // TickCoolTimes 내부에서 키를 임시 보관하는 재사용 버퍼
    private readonly List<int> _keyBuffer = new List<int>();

    // ── 생성자: VContainer가 SkillData[] 주입 ──────
    public SkillManager(SkillData[] allSkills)
    {
        _skillDict       = new Dictionary<int, SkillData>(allSkills.Length);
        _skillsByType    = new Dictionary<SkillType, List<SkillData>>();
        _allSkillsList   = new List<SkillData>(allSkills.Length);
        _coolTimeTracker = new Dictionary<BaseCharacter, Dictionary<int, int>>();

        foreach (SkillData skill in allSkills)
        {
            if (skill.SkillId == 0)
            {
                Debug.LogWarning($"[SkillManager] SkillId가 0인 에셋 발견: {skill.name}");
                continue;
            }

            _skillDict[skill.SkillId] = skill;
            _allSkillsList.Add(skill);

            // 타입별 목록 미리 구성
            if (_skillsByType.ContainsKey(skill.Type) == false)
                _skillsByType[skill.Type] = new List<SkillData>();
            _skillsByType[skill.Type].Add(skill);
        }

        Debug.Log($"[SkillManager] 초기화 완료 — 등록된 스킬 수: {_skillDict.Count}");
    }

    // ── ISkillManager 구현 ────────────────────────

    public SkillData GetSkill(int skillId)
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
    public void RegisterCoolTime(BaseCharacter character, int skillId, int coolTime)
    {
        if (_coolTimeTracker.ContainsKey(character) == false)
            _coolTimeTracker[character] = new Dictionary<int, int>();

        _coolTimeTracker[character][skillId] = coolTime;
        Debug.Log($"[SkillManager] 쿨타임 등록: {character.name} / {skillId} = {coolTime}턴");
    }

    // 턴마다 쿨타임 감소 — _keyBuffer 재사용으로 할당 없음
    public void TickCoolTimes(BaseCharacter character)
    {
        if (_coolTimeTracker.TryGetValue(character, out Dictionary<int, int> coolTimes) == false) return;

        _keyBuffer.Clear();
        foreach (int key in coolTimes.Keys)
            _keyBuffer.Add(key);

        foreach (int key in _keyBuffer)
        {
            if (coolTimes[key] > 0)
                coolTimes[key]--;
        }
    }

    // 남은 쿨타임 반환 로직
    public int GetRemainingCoolTime(BaseCharacter character, int skillId)
    {
        if (_coolTimeTracker.TryGetValue(character, out Dictionary<int, int> coolTimes))
            if (coolTimes.TryGetValue(skillId, out int remaining))
                return remaining;
        return 0;
    }
}
