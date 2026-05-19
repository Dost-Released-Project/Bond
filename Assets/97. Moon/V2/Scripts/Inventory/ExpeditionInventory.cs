using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

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

    public override async Task<int> AddItemId(string id, int quantity)
    {
        // 1. 아이템 DB 로드
        var conHandle = Addressables.LoadAssetAsync<ConsumableDataBaseSO>("ConsumableDataBase");
        var accHandle = Addressables.LoadAssetAsync<AccessoryDataBaseSO>("AccessoryDataBase");
        await System.Threading.Tasks.Task.WhenAll(conHandle.Task, accHandle.Task);
        
        var accDB = accHandle.Result;
        var conDB = conHandle.Result;
        
        var item = accDB.GetSO<BaseItem>(id);
        // 없으면 소모품 DB에서 검색
        if (item == null)
        {
            item = conDB.GetSO<BaseItem>(id);
        }
        
        return item != null ? AddItemAuto(item, quantity) : quantity;
    }
}