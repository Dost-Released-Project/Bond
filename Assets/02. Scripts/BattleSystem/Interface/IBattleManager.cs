using PipeLine;
using Cysharp.Threading.Tasks;
using System.Collections.Generic;

namespace BattleSystem.Interface
{
    public interface IBattleManager
    {
        public UniTask StartFocusEffect(CharacterSlot caster, List<CharacterSlot> targets);
        public UniTask EndFocusEffect(CharacterSlot caster, List<CharacterSlot> targets);
    }
}
