using System;

public abstract class BattleEventArgs : System.EventArgs
{
}

public class AttackEventArgs : BattleEventArgs
{
    public BaseCharacter Attacker { get; set; }
    public BaseCharacter Target { get; set; }
    public Action Skill { get; set; }
}