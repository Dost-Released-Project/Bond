using System.Collections.Generic;
using UnityEngine;

// 3. 서포터: SUPPORT_SPELL 선호
public class AutoBattle_Sup : AutoBattle
{
    public override void BattleAction(SkillBase[] skills)
    {
        if (isPlayable) return;
        var favorites = new List<SkillType> { SkillType.SUPPORT_SPELL };
        DecideSkill(skills, favorites)?.UseSkill();
    }
    public AutoBattle_Sup(string str) { Debug.Log($"{str}: 서포터 세팅"); }
}