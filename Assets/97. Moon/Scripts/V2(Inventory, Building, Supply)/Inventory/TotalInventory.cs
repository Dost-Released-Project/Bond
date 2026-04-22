using System.Linq;
using UnityEngine;

public class TotalInventory : InventoryBase, ITotalInventory
{
    private readonly ResourceManager _resourceManager;
    public  int GlobalItemLimit = 10; // 붕대 예시: 10개

    public TotalInventory(int capacity, ResourceManager resourceManager) : base(capacity)
    {
        _resourceManager = resourceManager;
    }

    // 창고는 아이템의 totalGlobalMax를 기준으로 스택 결정
    protected override int GetStackLimit(BaseItem item) => item.totalGlobalMax;

    public override void AddItem(BaseItem item, int quantity)
    {
        int currentTotal = _slots.Where(s => s.item?.id == item.id).Sum(s => s.quantity);
        int canAdd = GlobalItemLimit - currentTotal;

        if (canAdd <= 0)
        {
            ProcessExcessToLog(item, quantity);
            return;
        }

        int actualToInventory = Mathf.Min(quantity, canAdd);
        int excess = quantity - actualToInventory;

        base.AddItem(item, actualToInventory);

        if (excess > 0) ProcessExcessToLog(item, excess);
    }

    public void ProcessExcessToLog(BaseItem item, int quantity)
    {
        // 아이템 1개당 개척 데이터 5로 변환 (예시)
        _resourceManager.AddFrontierData(quantity * 5);
        Debug.Log($"{item.itemName} 초과분 {quantity}개가 개척 데이터로 전환되었습니다.");
    }
    
    public void ExpandStorage(int additionalSlots) { /* 생략 */ }
}