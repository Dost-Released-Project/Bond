using System.Collections.Generic;
using UnityEngine;

// 1. 딜러: OFFENSIVE, OFFENSIVE_SPELL 선호
public class AutoBattle_Atk : AutoBattle
{
    public override void BattleAction(SkillBase[] skills)
    {
        if (isPlayable) return;
        var favorites = new List<SkillType> { SkillType.OFFENSIVE, SkillType.OFFENSIVE_SPELL };
        DecideSkill(skills, favorites)?.UseSkill();
    }
    public AutoBattle_Atk(string str) { Debug.Log($"{str}: 딜러 세팅"); }
}