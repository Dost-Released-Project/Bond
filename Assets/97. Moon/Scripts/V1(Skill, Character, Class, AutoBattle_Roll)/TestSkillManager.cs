using System.Collections.Generic;
using System.Linq;
using UnityEngine;
/*
public class TestSkillManager : MonoBehaviour, ISkillManager
{
    // 1. 인스펙터에서 모든 SkillData(ScriptableObject)를 드래그해서 넣어주세요.
    [Header("스킬 데이터베이스")]
    [SerializeField] private List<SkillData> _skillDatabase = new List<SkillData>();

    // 빠른 검색을 위한 사전 (ID를 키로 사용)
    private Dictionary<string, SkillData> _skillDict;

    private void Awake()
    {
        // 리스트를 사전으로 변환하여 검색 효율을 높입니다 (O(1))
        _skillDict = _skillDatabase
            .Where(s => s != null)
            .ToDictionary(s => s.SkillId, s => s);
    }

    // ── 데이터 조회 구현 (필수 사항) ───────────────────────────────

    /// <summary>ID로 특정 스킬 데이터를 가져옵니다.</summary>
    public SkillData GetSkill(string skillId)
    {
        if (_skillDict != null && _skillDict.TryGetValue(skillId, out var skill))
        {
            return skill;
        }

        Debug.LogWarning($"[SkillManager] ID {skillId}에 해당하는 스킬을 찾을 수 없습니다.");
        return null;
    }

    /// <summary>특정 타입(공격, 방어 등)에 해당하는 모든 스킬 목록을 반환합니다.</summary>
    public IReadOnlyList<SkillData> GetSkillsByType(SkillType type)
    {
        return _skillDatabase
            .Where(s => s.Type == type)
            .ToList();
    }

    /// <summary>등록된 모든 스킬 목록을 반환합니다.</summary>
    public IReadOnlyList<SkillData> GetAllSkills()
    {
        return _skillDatabase.AsReadOnly();
    }

    public void RegisterCoolTime(BaseCharacter character, string skillId, int coolTime)
    {
        throw new System.NotImplementedException();
    }

    // ── 쿨타임 관리 (사용하지 않으므로 비워둠) ───────────────────────

    public void TickCoolTimes(BaseCharacter character) { }
    public int GetRemainingCoolTime(BaseCharacter character, string skillId)
    {
        throw new System.NotImplementedException();
    }
}*/