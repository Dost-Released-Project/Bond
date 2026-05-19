using System;
using System.Collections.Generic;
using UnityEngine;

// 2. 탱커: DEFENSIVE 선호
[Serializable]
public class AutoBattle_Def : AutoBattle
{
    public override SkillBase BattleAction(SkillBase[] skills)
    {
        if (isPlayable) return null;
        var favorites = new List<SkillType> { SkillType.DEFENSIVE };
        return DecideSkill(skills, favorites);
    }
    public AutoBattle_Def() { }
    public AutoBattle_Def(string str) { Debug.Log($"{str}: 탱커 세팅"); }
}