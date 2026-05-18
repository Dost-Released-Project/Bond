using System;
using System.Collections.Generic;
using Bond.Expedition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// IEventEffectApplier 구현체.
/// RootScope Singleton인 ExpeditionPayload를 통해 파티(Party)와 원정 인벤토리(Supplies)에 접근한다.
/// 미구현 시스템(상태이상, 아이템 DB)은 TODO 주석으로 스텁 처리한다.
/// </summary>
public class EventEffectApplier : IEventEffectApplier
{
    private readonly ExpeditionPayload _expeditionPayload;

    /// <summary>
    /// VContainer 생성자 주입.
    /// ExpeditionPayload 는 RootScope Singleton 이므로 MapLifetimeScope 에서 접근 가능하다.
    /// </summary>
    [Inject]
    public EventEffectApplier(ExpeditionPayload expeditionPayload)
    {
        _expeditionPayload = expeditionPayload;
    }

    /// <summary>
    /// 효과 타입에 따라 분기해 실제 시스템에 적용하고, 완료 후 onCompleted 를 호출한다.
    /// </summary>
    /// <param name="effect">적용할 효과 데이터.</param>
    /// <param name="onCompleted">효과 적용 완료 후 실행할 콜백. null 허용.</param>
    public async UniTask ApplyAsync(EventEffectData effect, Action onCompleted)
    {
        if (effect == null)
        {
            onCompleted?.Invoke();
            return;
        }

        switch (effect.EffectType)
        {
            case EffectType.HpChange:
                await ApplyHpChangeAsync(effect);
                break;
            case EffectType.StatusEffect:
                await ApplyStatusEffectAsync(effect);
                break;
            case EffectType.ItemReward:
                ApplyItemReward(effect);
                break;
            case EffectType.Battle:
                // Battle 타입은 EventSceneController 에서 HandleBattleChoice() 로 처리된다.
                // ApplyAsync 로 전달되면 설계 오류이므로 예외를 발생시킨다.
                throw new InvalidOperationException("[EventEffectApplier] EffectType.Battle 은 ApplyAsync 로 처리하지 않습니다.");
            case EffectType.None:
                break;
            default:
                Debug.LogWarning($"[EventEffectApplier] 처리되지 않은 EffectType: {effect.EffectType}");
                break;
        }

        onCompleted?.Invoke();
    }

    /// <summary>
    /// HP 변화 효과를 TargetType 에 따라 분기해 적용한다.
    /// </summary>
    private async UniTask ApplyHpChangeAsync(EventEffectData effect)
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
                // TODO: 파티 선택 UI 를 활성화하고 플레이어 입력을 대기한다.
                // UI 시스템 구현 완료 후 UniTaskCompletionSource 로 대기 로직 추가.
                Debug.LogWarning("[EventEffectApplier] ChooseOne UI 미구현 — 랜덤으로 대체 적용");
                ApplyHpToRandom(effect.HpChangeAmount);
                break;
            default:
                break;
        }

        await UniTask.CompletedTask;
    }

    /// <summary>
    /// 파티 전원에게 HP 변화를 적용한다.
    /// ExpeditionPayload.Party 를 통해 현재 원정 파티 구성원에 접근한다.
    /// </summary>
    private void ApplyHpToAll(int amount)
    {
        IReadOnlyList<BaseCharacter> party = _expeditionPayload.Party;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("[EventEffectApplier] ApplyHpToAll: 파티 데이터가 없습니다.");
            return;
        }

        foreach (BaseCharacter character in party)
        {
            if (amount > 0)
                character.RecoverHp(amount);
            else
                character.ReduceHP(-amount);

            Debug.Log($"[EventEffectApplier] HP 변화 적용: {character.Name}, amount={amount}");
        }
    }

    /// <summary>
    /// 파티 중 랜덤 1명에게 HP 변화를 적용한다.
    /// ExpeditionPayload.Party 를 통해 현재 원정 파티 구성원에 접근한다.
    /// </summary>
    private void ApplyHpToRandom(int amount)
    {
        IReadOnlyList<BaseCharacter> party = _expeditionPayload.Party;
        if (party == null || party.Count == 0)
        {
            Debug.LogWarning("[EventEffectApplier] ApplyHpToRandom: 파티 데이터가 없습니다.");
            return;
        }

        int index = UnityEngine.Random.Range(0, party.Count);
        BaseCharacter target = party[index];

        if (amount > 0)
            target.RecoverHp(amount);
        else
            target.ReduceHP(-amount);

        Debug.Log($"[EventEffectApplier] 랜덤 HP 변화 적용: {target.Name}, amount={amount}");
    }

    /// <summary>
    /// 상태이상 효과를 적용한다. 상태이상 시스템 미구현으로 스텁 처리.
    /// </summary>
    private async UniTask ApplyStatusEffectAsync(EventEffectData effect)
    {
        // TODO: 상태이상 시스템 구현 후 연동
        Debug.LogWarning($"[EventEffectApplier] 상태이상 적용 미구현: {effect.StatusEffectId}");
        await UniTask.CompletedTask;
    }

    /// <summary>
    /// 아이템 보상을 ItemRewardType 에 따라 분기해 적용한다.
    /// </summary>
    private void ApplyItemReward(EventEffectData effect)
    {
        switch (effect.ItemRewardType)
        {
            case ItemRewardType.Guaranteed:
                AddItemById(effect.GuaranteedItemId, 1);
                break;
            case ItemRewardType.Probability:
                float roll = UnityEngine.Random.value;
                if (roll <= effect.ItemProbability)
                    AddItemById(effect.ProbabilityItemId, 1);
                else
                    Debug.Log("[EventEffectApplier] 아이템 획득 실패 (확률 미달성)");
                break;
            case ItemRewardType.RandomFromPool:
                if (effect.ItemPool == null || effect.ItemPool.Count == 0)
                {
                    Debug.LogWarning("[EventEffectApplier] ItemPool 이 비어 있습니다.");
                    return;
                }
                int poolIndex = UnityEngine.Random.Range(0, effect.ItemPool.Count);
                AddItemById(effect.ItemPool[poolIndex], 1);
                break;
            default:
                break;
        }
    }

    /// <summary>
    /// 아이템 ID 로 아이템을 원정 인벤토리에 추가한다.
    /// ItemDatabase(SO 레지스트리) 미구현으로 현재는 로그 출력 스텁.
    /// </summary>
    private void AddItemById(string itemId, int quantity)
    {
        // TODO: ItemDatabase(SO 레지스트리) 구현 후 itemId → BaseItem 변환 추가
        // IItemDatabase.Get(itemId) 로 BaseItem 을 획득한 뒤 아래 주석 해제
        // _expeditionPayload.Supplies.AddItemAuto(item, quantity);
        Debug.Log($"[EventEffectApplier] 아이템 획득 (ItemDB 미구현): id={itemId}, qty={quantity}");
    }
}
