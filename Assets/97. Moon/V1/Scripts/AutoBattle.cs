using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public abstract class AutoBattle
{
    public bool isPlayable;
    public abstract SkillBase BattleAction(SkillBase[] skills);

    // 확률에 따라 선호 스킬 또는 일반 스킬을 선택하는 공통 로직
    protected SkillBase DecideSkill(SkillBase[] skills, List<SkillType> favoriteTypes)
    {
        if (skills == null || skills.Length == 0) return null;

        // 내 역할군에 맞는 '선호 스킬' 리스트 필터링
        var favoriteSkills = skills.Where(s => s != null && favoriteTypes.Contains(GetSkillType(s))).ToList();

        // 70% 확률로 선호 스킬 사용, 아니면 그냥 아무거나 사용
        if (favoriteSkills.Count > 0 && UnityEngine.Random.value < 0.7f)
        {
            return favoriteSkills[UnityEngine.Random.Range(0, favoriteSkills.Count)];
        }

        return skills[UnityEngine.Random.Range(0, skills.Length)];
    }

    // SkillBase에서 Type을 안전하게 가져오기 위한 헬퍼 (캐스팅 처리)
    private SkillType GetSkillType(SkillBase skill)
    {
        return SkillType.OFFENSIVE; // 기본값
    }
}