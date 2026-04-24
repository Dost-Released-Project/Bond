using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class ExpeditionInventory : InventoryBase, IExpeditionInventory
{
    public ExpeditionInventory(int capacity) : base(capacity) { }
    
    // 탐사 가방은 아이템의 expeditionSlotMax를 기준으로 스택 결정
    public override int GetStackLimit(BaseItem item) => item.expeditionSlotMax;

    public override int AddItemAuto(BaseItem item, int quantity)
    {
        // 1. 기존 스택이 있고 여유가 있는 슬롯부터 찾기
        for (int i = 0; i < _slots.Count; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].item.id == item.id && _slots[i].quantity < GetStackLimit(item))
            {
                return AddItemAt(i, item, quantity);
            }
        }
        // 2. 빈 슬롯 찾기
        int emptyIdx = _slots.FindIndex(s => s.IsEmpty);
        if (emptyIdx != -1) return AddItemAt(emptyIdx, item, quantity);

        return quantity;
    }

    // 에러 수정된 부분: Count()를 사용하거나 .Count 프로퍼티 사용
    public (int used, int max) GetSlotStatus() => (_slots.Count(s => !s.IsEmpty), Capacity);

    public bool EquipItem(BaseCharacter character, string itemID) => true;
    public void UnequipItem(BaseCharacter character, Equipment slot) { }
}