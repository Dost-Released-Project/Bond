using BattleSystem;

namespace BattleSystem.Interface
{
    public interface ISlot
    {
        FormationMask rank { get; }
        E_BattleSide side { get; }
    }
}
