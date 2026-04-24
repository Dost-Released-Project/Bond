using UnityEngine;

public class TotalInventory : InventoryBase, ITotalInventory
{
    private readonly ResourceManager _resourceManager;
    public int GlobalItemLimit = 10;
    public TotalInventory(int capacity, ResourceManager rm) : base(capacity) { _resourceManager = rm; }
    
    public override int GetStackLimit(BaseItem item) => item.totalGlobalMax;

    public override int AddItemAuto(BaseItem item, int quantity)
    {
        // 1. 기존 슬롯 찾기
        int existingIdx = _slots.FindIndex(s => !s.IsEmpty && s.item.id == item.id);
        if (existingIdx != -1) return AddItemAt(existingIdx, item, quantity);
        
        // 2. 없으면 첫 빈 슬롯
        int emptyIdx = _slots.FindIndex(s => s.IsEmpty);
        if (emptyIdx != -1) return AddItemAt(emptyIdx, item, quantity);
        
        return quantity;
    }

    // 드래그로 특정 위치에 놓을 때도 중복 체크
    public override int AddItemAt(int index, BaseItem item, int quantity)
    {
        // 1. 현재 인벤토리의 '다른 슬롯'에 이미 같은 아이템이 있는지 확인
        int existingIdx = _slots.FindIndex(s => !s.IsEmpty && s.item.id == item.id);
    
        // 2. 다른 곳에 있다면 그쪽으로 강제 합류 (중복 방지)
        if (existingIdx != -1 && existingIdx != index)
        {
            return InternalAdd(existingIdx, item, quantity);
        }

        // 3. 없다면 지정된 위치(index)에 추가
        return InternalAdd(index, item, quantity);
    }

    private int InternalAdd(int index, BaseItem item, int quantity)
    {
        int limit = GetStackLimit(item);
        var slot = _slots[index];

        // 슬롯이 비어있으면 초기화
        if (slot.IsEmpty)
        {
            slot.item = item;
            slot.quantity = 0; // 수량을 0으로 확실히 초기화 후 시작
        }

        int canAdd = limit - slot.quantity;
        int actualAdd = Mathf.Min(quantity, canAdd);
        int excess = quantity - actualAdd;

        slot.quantity += actualAdd; // 여기서 기존 수량에 더함

        if (excess > 0)
        {
            ProcessExcessToLog(item, excess);
        }
    
        return 0; // 초과분은 로그로 빠졌으므로 0 반환
    }

    public void ProcessExcessToLog(BaseItem item, int quantity)
    {
        _resourceManager.AddFrontierData(quantity * 5);
        Debug.Log($"<color=cyan>[로그]</color> {item.itemName} 초과분 {quantity}개가 개척 데이터로 전환되었습니다.");
    }
}