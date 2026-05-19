using System;
using System.Collections.Generic;
using UnityEngine;

// 1. 딜러: OFFENSIVE, OFFENSIVE_SPELL 선호
[Serializable]
public class AutoBattle_Atk : AutoBattle
{
    public override SkillBase BattleAction(SkillBase[] skills)
    {
        if (isPlayable) return null;
        var favorites = new List<SkillType> { SkillType.OFFENSIVE, SkillType.SPELL };
        
        return DecideSkill(skills, favorites);
    }
    public AutoBattle_Atk() { }
    public AutoBattle_Atk(string str) { Debug.Log($"{str}: 딜러 세팅"); }
}