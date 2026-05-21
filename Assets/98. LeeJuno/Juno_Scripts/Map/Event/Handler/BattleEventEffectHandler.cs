using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// EffectType.Battle 을 ApplyAsync 로 처리하려 할 때 예외를 발생시키는 핸들러.
/// Battle 타입은 EventSceneController.HandleBattleChoice() 에서만 처리해야 한다.
/// </summary>
public class BattleEventEffectHandler : IEventEffectHandler
{
    public bool CanHandle(EffectType effectType) => effectType == EffectType.Battle;

    public UniTask HandleAsync(EventEffectData effect)
    {
        throw new InvalidOperationException(
            "[BattleEventEffectHandler] EffectType.Battle 은 ApplyAsync 로 처리하지 않습니다. " +
            "EventSceneController.HandleBattleChoice() 를 사용하십시오.");
    }
}
