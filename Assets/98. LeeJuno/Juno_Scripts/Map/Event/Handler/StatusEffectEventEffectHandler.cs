using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// EffectType.StatusEffect 를 처리하는 핸들러.
/// 상태이상 시스템 미구현으로 스텁 처리된다.
/// </summary>
public class StatusEffectEventEffectHandler : IEventEffectHandler
{
    public bool CanHandle(EffectType effectType) => effectType == EffectType.StatusEffect;

    public async UniTask HandleAsync(EventEffectData effect)
    {
        // TODO: 상태이상 시스템 구현 후 연동
        Debug.LogWarning($"[StatusEffectEventEffectHandler] 상태이상 적용 미구현: {effect.StatusEffectId}");
        await UniTask.CompletedTask;
    }
}
