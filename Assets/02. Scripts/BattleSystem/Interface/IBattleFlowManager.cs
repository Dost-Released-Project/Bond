using System;
using UnityEngine;

public interface IBattleFlowManager
{
    event Action<BaseCharacter[], BaseCharacter[]> OnBattleStart;
}
