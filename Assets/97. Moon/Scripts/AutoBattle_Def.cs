using System.Collections.Generic;
using UnityEngine;

// 2. 탱커: DEFENSIVE 선호
public class AutoBattle_Def : AutoBattle
{
    public override void BattleAction(SkillBase[] skills)
    {
        if (isPlayable) return;
        var favorites = new List<SkillType> { SkillType.DEFENSIVE };
        DecideSkill(skills, favorites)?.UseSkill();
    }
    public AutoBattle_Def(string str) { Debug.Log($"{str}: 탱커 세팅"); }
}