using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// 테스트 환경에서 IEventEffectApplier 를 대체하는 스텁.
/// 효과 적용 로직 없이 로그만 출력하고 즉시 완료한다.
/// </summary>
public class NullEventEffectApplier : IEventEffectApplier
{
    public UniTask ApplyAsync(EventEffectData effect, Action onCompleted)
    {
        Debug.Log($"[NullEventEffectApplier] ApplyAsync 호출됨 — EffectType: {effect?.EffectType}");
        onCompleted?.Invoke();
        return UniTask.CompletedTask;
    }
}
