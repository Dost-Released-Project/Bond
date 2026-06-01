using Bond.Expedition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// EffectType.ItemReward 를 처리하는 핸들러.
/// 아이템 ID 를 확정하고 ExpeditionInventory 에 실제로 추가한다.
/// 확정된 아이템 표시 이름을 EventLogAccumulator 에 기록해 OutcomeDescription 앞에 붙일 수 있게 한다.
/// </summary>
public class ItemRewardEventEffectHandler : IEventEffectHandler
{
    private readonly EventLogAccumulator _logAccumulator;
    private readonly ExpeditionPayload _expeditionPayload;
    private readonly MapConfigCache _mapConfigCache;

    [Inject]
    public ItemRewardEventEffectHandler(
        EventLogAccumulator logAccumulator,
        ExpeditionPayload expeditionPayload,
        MapConfigCache mapConfigCache)
    {
        _logAccumulator    = logAccumulator;
        _expeditionPayload = expeditionPayload;
        _mapConfigCache    = mapConfigCache;
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
            BaseItem item = GetItemById(resolvedItemId);

            if (item != null)
            {
                AddItemToInventory(item, 1);
                _logAccumulator?.SetLastResolvedItemDisplayName(item.DisplayName);
            }
            else
            {
                Debug.LogWarning($"[ItemRewardEventEffectHandler] '{resolvedItemId}'를 AccessoryDB 에서 찾을 수 없습니다.");
                _logAccumulator?.SetLastResolvedItemDisplayName(resolvedItemId);
            }

            RecordItemLog(resolvedItemId);
        }
        else
        {
            // 아이템 미획득 — 예약된 이벤트 이름 및 아이템 이름을 초기화한다
            _logAccumulator?.ClearPendingEventName();
            _logAccumulator?.SetLastResolvedItemDisplayName(string.Empty);
        }

        return UniTask.CompletedTask;
    }

    /// <summary>
    /// AccessoryDB 에서 itemId 에 대응하는 BaseItem 을 조회한다.
    /// DB 가 없거나 항목을 찾지 못하면 null 을 반환한다.
    /// </summary>
    private BaseItem GetItemById(string itemId)
    {
        AccessoryDataBaseSO accessoryDB = _mapConfigCache?.AccessoryDB;
        if (accessoryDB == null)
        {
            Debug.LogWarning("[ItemRewardEventEffectHandler] AccessoryDB 가 없습니다.");
            return null;
        }

        return accessoryDB.GetSO<BaseItem>(itemId);
    }

    /// <summary>
    /// ExpeditionPayload.Supplies 에 아이템을 추가한다.
    /// 슬롯이 가득 찬 경우 남은 수량을 경고 로그로 출력한다.
    /// </summary>
    private void AddItemToInventory(BaseItem item, int quantity)
    {
        if (_expeditionPayload?.Supplies == null)
        {
            Debug.LogWarning("[ItemRewardEventEffectHandler] ExpeditionInventory 가 없습니다.");
            return;
        }

        int remaining = _expeditionPayload.Supplies.AddItemAuto(item, quantity);

        if (remaining > 0)
            Debug.LogWarning($"[ItemRewardEventEffectHandler] 인벤토리 가득 참 — {item.DisplayName} {remaining}개 미추가");
        else
            Debug.Log($"[ItemRewardEventEffectHandler] 아이템 획득: {item.DisplayName} x{quantity}");
    }

    /// <summary>
    /// DB 조회를 EventLogAccumulator 에 위임해 아이템 획득 로그를 기록한다.
    /// </summary>
    private void RecordItemLog(string itemId)
    {
        _logAccumulator?.FlushItemRewardLogById(itemId);
    }
}
