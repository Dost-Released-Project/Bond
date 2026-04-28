using UnityEngine;

public class TotalInventory : InventoryBase, ITotalInventory
{
    private readonly ResourceManager _resourceManager;
    public TotalInventory(int capacity, ResourceManager rm) : base(capacity) { _resourceManager = rm; }

    protected override int GetStackLimit(BaseItem item) => item.totalGlobalMax;
    
    public override int AddItemAuto(BaseItem item, int quantity)
    {
        if (item == null) return quantity;

        // 장신구가 아닐 때만 기존 스택을 찾음 (장신구는 중복/개별 슬롯 허용)
        if (item.category != ItemCategory.Accessories)
        {
            int existingIdx = _slots.FindIndex(s => !s.IsEmpty && s.item.id == item.id && s.quantity < GetStackLimit(item));
            if (existingIdx != -1) return AddItemAt(existingIdx, item, quantity);
        }
    
        // 빈 슬롯 찾기
        int emptyIdx = _slots.FindIndex(s => s.IsEmpty);
        if (emptyIdx != -1) return AddItemAt(emptyIdx, item, quantity);
    
        return quantity;
    }

    public override int AddItemAt(int index, BaseItem item, int quantity)
    {
        // [수정] 장신구가 아닐 때만 '다른 슬롯'의 동일 아이템과 합치기 수행
        if (item.category != ItemCategory.Accessories)
        {
            int existingIdx = _slots.FindIndex(s => !s.IsEmpty && s.item.id == item.id);
            if (existingIdx != -1 && existingIdx != index) index = existingIdx;
        }
    
        int limit = GetStackLimit(item);
        var slot = _slots[index];

        if (slot.IsEmpty) { slot.item = item; slot.quantity = 0; }

        int canAdd = limit - slot.quantity;
        int actualAdd = Mathf.Min(quantity, canAdd);
        int excess = quantity - actualAdd;

        slot.quantity += actualAdd;
        if (excess > 0) ProcessExcessToLog(item, excess);
    
        NotifyChanged();
        return 0;
    }
    
    public void ProcessExcessToLog(BaseItem item, int quantity)
    {
        _resourceManager.AddFrontierData(quantity * 5);
        Debug.Log($"<color=cyan>[로그]</color> {item.itemName} 초과분 {quantity}개 데이터 전환.");
    }
}