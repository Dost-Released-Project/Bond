using System;
using UnityEngine;

public interface IExpeditionFlowManager
{
    event Action<BaseCharacter[], BaseCharacter[]> OnBattleStart;
}
