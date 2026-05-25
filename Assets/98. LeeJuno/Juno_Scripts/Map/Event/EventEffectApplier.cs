using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// IEventEffectApplier 구현체.
/// switch 분기 대신 IEventEffectHandler Chain 에 처리를 위임한다.
/// 새 EffectType 추가 시 이 클래스를 수정하지 않고 Handler 만 추가하면 된다. (OCP 준수)
/// </summary>
public class EventEffectApplier : IEventEffectApplier
{
    private readonly IReadOnlyList<IEventEffectHandler> _handlers;

    [Inject]
    public EventEffectApplier(IReadOnlyList<IEventEffectHandler> handlers)
    {
        _handlers = handlers;
    }

    public async UniTask ApplyAsync(EventEffectData effect)
    {
        if (effect == null)
        {
            return;
        }

        bool handled = false;

        foreach (IEventEffectHandler handler in _handlers)
        {
            if (handler.CanHandle(effect.EffectType))
            {
                await handler.HandleAsync(effect);
                handled = true;
                break; // 첫 번째 매칭 핸들러에서 처리 후 중단
            }
        }

        if (handled == false)
        {
            Debug.LogWarning($"[EventEffectApplier] 처리되지 않은 EffectType: {effect.EffectType}");
        }
    }
}
