using System.Collections.Generic;
using UnityEngine;

// 3. 서포터: SUPPORT_SPELL 선호
public class AutoBattle_Sup : AutoBattle
{
    public override SkillBase BattleAction(SkillBase[] skills)
    {
        if (isPlayable) return null;
        var favorites = new List<SkillType> { SkillType.SUPPORT };
        return DecideSkill(skills, favorites);
    }
    public AutoBattle_Sup(string str) { Debug.Log($"{str}: 서포터 세팅"); }
}