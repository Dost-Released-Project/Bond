using UnityEngine;

public class TotalInventory : InventoryBase, ITotalInventory
{
    private readonly ResourceManager _resourceManager;
    public TotalInventory(int capacity, ResourceManager rm) : base(capacity) { _resourceManager = rm; }

    protected override int GetStackLimit(BaseItem item) => item.totalGlobalMax;

    public override int AddItemAuto(BaseItem item, int quantity)
    {
        // 0. 전달받은 아이템 자체가 null인지 체크
        if (item == null) {
            Debug.LogError("AddItemAuto: 추가하려는 아이템(item)이 null입니다!");
            return quantity;
        }
        
        int existingIdx = _slots.FindIndex(s => !s.IsEmpty && s.item != null && s.item.id == item.id && s.quantity < GetStackLimit(item));
        if (existingIdx != -1) return AddItemAt(existingIdx, item, quantity);
        
        int emptyIdx = _slots.FindIndex(s => s.IsEmpty);
        if (emptyIdx != -1) return AddItemAt(emptyIdx, item, quantity);
        
        
        return quantity;
    }

    public override int AddItemAt(int index, BaseItem item, int quantity)
    {
        // 중복 방지: 다른 슬롯에 이미 같은 아이템이 있으면 그쪽으로 합침 (index가 다를 때만)
        int existingIdx = _slots.FindIndex(s => !s.IsEmpty && s.item.id == item.id);
        if (existingIdx != -1 && existingIdx != index) index = existingIdx;
        
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