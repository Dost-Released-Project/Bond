using System;
using UnityEngine;

public interface IExpaditionFlowManager
{
    event Action<BaseCharacter[], BaseCharacter[]> OnBattleStart;
}
