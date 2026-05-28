using System.Collections.Generic;
using Bond.Expedition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// EffectType.HpChange 를 처리하는 핸들러.
/// ExpeditionPayload 를 통해 파티에 HP 변화를 적용한다.
/// </summary>
public class HpChangeEventEffectHandler : IEventEffectHandler
{
    private readonly ExpeditionPayload _expeditionPayload;

    [Inject]
    public HpChangeEventEffectHandler(ExpeditionPayload expeditionPayload)
    {
        _expeditionPayload = expeditionPayload;
    }

    public bool CanHandle(EffectType effectType) => effectType == EffectType.HpChange;

    public async UniTask HandleAsync(EventEffectData effect)
    {
        switch (effect.TargetType)
        {
            case TargetType.All:
                ApplyHpToAll(effect.HpChangeAmount);
                break;
            case TargetType.RandomOne:
                ApplyHpToRandom(effect.HpChangeAmount);
                break;
            case TargetType.ChooseOne:
                // TODO: 파티 선택 UI 활성화 후 UniTaskCompletionSource 로 대기
                Debug.LogWarning("[HpChangeEventEffectHandler] ChooseOne UI 미구현 — 랜덤으로 대체 적용");
                ApplyHpToRandom(effect.HpChangeAmount);
                break;
            default:
                break;
        }

        await UniTask.CompletedTask;
    }

    private void ApplyHpToAll(int amount)
    {
        IReadOnlyList<BaseCharacter> party = _expeditionPayload.Party;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("[HpChangeEventEffectHandler] ApplyHpToAll: 파티 데이터가 없습니다.");
            return;
        }

        foreach (BaseCharacter character in party)
        {
            if (amount > 0)
                character.RecoverHp(amount);
            else
                character.ReduceHP(-amount);

            Debug.Log($"[HpChangeEventEffectHandler] HP 변화 적용: {character.Name}, amount={amount}");
        }

    }

    private void ApplyHpToChooseOne(int amount, int index)
    {

    }

    private void ApplyHpToRandom(int amount)
    {
        IReadOnlyList<BaseCharacter> party = _expeditionPayload.Party;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("[HpChangeEventEffectHandler] ApplyHpToRandom: 파티 데이터가 없습니다.");
            return;
        }

        int index = UnityEngine.Random.Range(0, party.Count);
        BaseCharacter target = party[index];

        if (amount > 0)
            target.RecoverHp(amount);
        else
            target.ReduceHP(-amount);

        Debug.Log($"[HpChangeEventEffectHandler] 랜덤 HP 변화 적용: {target.Name}, amount={amount}");
    }
}