using _02._Scripts.BattleSystem;

public interface ISlot
{
    FormationMask rank { get; }
    E_BattleSide side { get; }
}
