using System;
using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class ExpeditionInventory : InventoryBase, IExpeditionInventory
{
    public ExpeditionInventory(int capacity) : base(capacity) { }
    protected override int GetStackLimit(BaseItem item) => item.expeditionSlotMax;

    public override int AddItemAuto(BaseItem item, int quantity)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            if (!_slots[i].IsEmpty && _slots[i].item.id == item.id && _slots[i].quantity < GetStackLimit(item))
                return AddItemAt(i, item, quantity);
        }
        int emptyIdx = _slots.FindIndex(s => s.IsEmpty);
        if (emptyIdx != -1) return AddItemAt(emptyIdx, item, quantity);
        return quantity;
    }
}