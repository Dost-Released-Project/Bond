using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// EffectType.ItemReward 를 처리하는 핸들러.
/// ItemDatabase 미구현으로 AddItemById 는 스텁 처리된다.
/// </summary>
public class ItemRewardEventEffectHandler : IEventEffectHandler
{
    public bool CanHandle(EffectType effectType) => effectType == EffectType.ItemReward;

    public UniTask HandleAsync(EventEffectData effect)
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
                    Debug.Log("[ItemRewardEventEffectHandler] 아이템 획득 실패 (확률 미달성)");
                break;
            case ItemRewardType.RandomFromPool:
                if (effect.ItemPool == null || effect.ItemPool.Count == 0)
                {
                    Debug.LogWarning("[ItemRewardEventEffectHandler] ItemPool 이 비어 있습니다.");
                    break;
                }
                int poolIndex = UnityEngine.Random.Range(0, effect.ItemPool.Count);
                AddItemById(effect.ItemPool[poolIndex], 1);
                break;
            default:
                break;
        }

        return UniTask.CompletedTask;
    }

    private void AddItemById(string itemId, int quantity)
    {
        // TODO: ItemDatabase 구현 후 itemId → BaseItem 변환 추가
        Debug.Log($"[ItemRewardEventEffectHandler] 아이템 획득 (ItemDB 미구현): id={itemId}, qty={quantity}");
    }
}
