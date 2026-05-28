using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// EffectType.ItemReward 를 처리하는 핸들러.
/// 아이템 획득 확정 시 EventLogAccumulator 에 로그를 기록한다.
/// DB 조회는 EventLogAccumulator.FlushItemRewardLogById() 에 위임한다.
/// ItemDatabase 미구현으로 AddItemById 는 스텁 처리된다.
/// </summary>
public class ItemRewardEventEffectHandler : IEventEffectHandler
{
    private readonly EventLogAccumulator _logAccumulator;

    public ItemRewardEventEffectHandler(EventLogAccumulator logAccumulator)
    {
        _logAccumulator = logAccumulator;
    }

    public bool CanHandle(EffectType effectType) => effectType == EffectType.ItemReward;

    public UniTask HandleAsync(EventEffectData effect)
    {
        string resolvedItemId = string.Empty;

        switch (effect.ItemRewardType)
        {
            case ItemRewardType.Guaranteed:
                resolvedItemId = effect.GuaranteedItemId;
                break;

            case ItemRewardType.Probability:
                float roll = UnityEngine.Random.value;
                if (roll <= effect.ItemProbability)
                    resolvedItemId = effect.ProbabilityItemId;
                else
                    Debug.Log("[ItemRewardEventEffectHandler] 아이템 획득 실패 (확률 미달성)");
                break;

            case ItemRewardType.RandomFromPool:
                if (effect.ItemPool == null || effect.ItemPool.Count == 0)
                {
                    Debug.LogWarning("[ItemRewardEventEffectHandler] ItemPool 이 비어 있습니다.");
                    break;
                }
                int poolIndex = UnityEngine.Random.Range(0, effect.ItemPool.Count);
                resolvedItemId = effect.ItemPool[poolIndex];
                break;

            default:
                break;
        }

        if (string.IsNullOrEmpty(resolvedItemId) == false)
        {
            AddItemById(resolvedItemId, 1);
            RecordItemLog(resolvedItemId);
        }
        else
        {
            // 아이템 미획득 — 예약된 이벤트 이름을 초기화한다
            _logAccumulator?.ClearPendingEventName();
        }

        return UniTask.CompletedTask;
    }

    private void AddItemById(string itemId, int quantity)
    {
        // TODO: ItemDatabase 구현 후 itemId → BaseItem 변환 추가
        Debug.Log($"[ItemRewardEventEffectHandler] 아이템 획득 (ItemDB 미구현): id={itemId}, qty={quantity}");
    }

    /// <summary>
    /// DB 조회를 EventLogAccumulator 에 위임해 아이템 획득 로그를 기록한다.
    /// </summary>
    /// <param name="itemId">획득한 아이템 ID.</param>
    private void RecordItemLog(string itemId)
    {
        _logAccumulator?.FlushItemRewardLogById(itemId);
    }
}
