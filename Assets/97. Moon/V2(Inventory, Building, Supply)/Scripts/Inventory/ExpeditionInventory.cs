using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class ExpeditionInventory : InventoryBase, IExpeditionInventory
{
    public ExpeditionInventory(int capacity) : base(capacity) { }

    // 슬롯당 적재 수량 제한 (탐사용은 작게 설정, 예: 2)
    public int ExpeditionStackLimit = 2;
    
    // 탐사 가방은 아이템의 expeditionSlotMax를 기준으로 스택 결정
    public override int GetStackLimit(BaseItem item) => item.expeditionSlotMax;

    // public override void AddItem(BaseItem item, int quantity)
    // {
    //     // 탐사 인벤토리는 슬롯당 제한(2개)을 지키며 여러 슬롯에 나눠 담음
    //     while (quantity > 0)
    //     {
    //         var slot = _slots.Find(s => s.item?.id == item.id && s.quantity < ExpeditionStackLimit) 
    //                    ?? _slots.Find(s => s.IsEmpty);
    //
    //         if (slot == null) break; // 더 이상 담을 공간 없음
    //
    //         if (slot.IsEmpty) slot.item = item;
    //         int addable = ExpeditionStackLimit - slot.quantity;
    //         int actualAdd = Mathf.Min(quantity, addable);
    //         slot.quantity += actualAdd;
    //         quantity -= actualAdd;
    //     }
    // }

    public override int AddItem(BaseItem item, int quantity)
    {
        return base.AddItem(item, quantity);
    }

    // 에러 수정된 부분: Count()를 사용하거나 .Count 프로퍼티 사용
    public (int used, int max) GetSlotStatus() => (_slots.Count(s => !s.IsEmpty), Capacity);

    public bool EquipItem(BaseCharacter character, string itemID) => true;
    public void UnequipItem(BaseCharacter character, Equipment slot) { }
}