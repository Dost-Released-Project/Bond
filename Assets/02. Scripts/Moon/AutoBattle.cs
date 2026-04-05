using UnityEngine;

public abstract class AutoBattle
{
    public bool isPlayable;
    public abstract void BattleAction(SkillBase skill);
}
