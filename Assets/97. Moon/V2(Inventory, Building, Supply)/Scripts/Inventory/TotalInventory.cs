using UnityEngine;

public class TotalInventory : InventoryBase, ITotalInventory
{
    private readonly ResourceManager _resourceManager;
    public int GlobalItemLimit = 10;

    public TotalInventory(int capacity, ResourceManager resourceManager) : base(capacity) { _resourceManager = resourceManager; }

    public override int GetStackLimit(BaseItem item) => item.totalGlobalMax;

    // public override int AddItem(BaseItem item, int quantity)
    // {
    //     // [요구사항 3] TotalInventory는 무조건 기존 슬롯을 찾거나 첫 번째 가능한 슬롯 하나만 사용
    //     var slot = _slots.Find(s => s.item?.id == item.id) ?? _slots.Find(s => s.IsEmpty);
    //     
    //     if (slot == null) return quantity; // 공간 없음
    //
    //     if (slot.IsEmpty) slot.item = item;
    //     
    //     int currentTotal = slot.quantity;
    //     int canAdd = GlobalItemLimit - currentTotal;
    //     int actualAdd = Mathf.Min(quantity, canAdd);
    //     int excess = quantity - actualAdd;
    //
    //     slot.quantity += actualAdd;
    //
    //     if (excess > 0) ProcessExcessToLog(item, excess);
    //     return 0; // 초과분은 로그로 처리했으므로 잔량 0 반환
    // }
    
    public override int AddItem(BaseItem item, int quantity)
    {
        // [버그 수정] 같은 아이템이 2개 생기지 않도록 '무조건' 기존 슬롯부터 찾습니다.
        var existingSlot = _slots.Find(s => !s.IsEmpty && s.item.id == item.id);
    
        if (existingSlot != null)
        {
            int canAdd = GlobalItemLimit - existingSlot.quantity;
            int actualAdd = Mathf.Min(quantity, canAdd);
            existingSlot.quantity += actualAdd;
        
            int excess = quantity - actualAdd;
            if (excess > 0) ProcessExcessToLog(item, excess);
            return 0; // 초과분은 로그 처리했으므로 소모된 것으로 간주
        }
        
        return base.AddItem(item, quantity);
    }

    public void ProcessExcessToLog(BaseItem item, int quantity)
    {
        _resourceManager.AddFrontierData(quantity * 5);
        Debug.Log($"<color=cyan>[로그]</color> {item.itemName} 초과분 {quantity}개가 개척 데이터로 전환되었습니다.");
    }
    public void ExpandStorage(int additionalSlots) { /* 로직 */ }
}