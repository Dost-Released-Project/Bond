using UnityEngine;

public enum ConsumableType { Bandage, Sedative, Awakening }

[CreateAssetMenu(fileName = "NewConsumable", menuName = "Items/Consumable")]
public class ConsumableItem : BaseItem
{
    public ConsumableType consumableType;
    public int healValue = 20;

    private void OnEnable()
    {
        category = ItemCategory.Consume;
    }

    public override void Use(BaseCharacter target)
    {
        if (target == null) return;
        
        if (!target.TryGetComponent<Stat>(out var stat)) return;

        switch (consumableType)
        {
            case ConsumableType.Bandage:
                stat.RecoverHp(healValue);
                Debug.Log($"[사용] {itemName}: {target.name}의 HP 회복");
                break;
            case ConsumableType.Sedative:
                stat.RecoverInsanity(healValue);
                Debug.Log($"[사용] {itemName}: {target.name}의 스트레스 감소");
                break;
            case ConsumableType.Awakening:
                // 성향 시스템은 팀원이 작업 중이므로 연동 지점만 만듭니다.
                TryAwakenTrait(target);
                break;
        }
    }

    private void TryAwakenTrait(BaseCharacter target)
    {
        // TODO: 팀원이 만든 성향 시스템 클래스(예: TraitSystem)를 가져와서 호출
        // target.GetComponent<TraitSystem>().UnlockRandomTrait();
        Debug.Log($"[사용] {itemName}: {target.name}의 잠재 성향 하나가 깨어납니다. (성향 시스템 연동 대기)");
    }
}