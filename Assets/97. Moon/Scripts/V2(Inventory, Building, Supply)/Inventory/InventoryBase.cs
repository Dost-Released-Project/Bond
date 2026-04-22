using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class InventoryBase : IInventory
{
    protected List<InventorySlot> _slots;
    // 인터페이스 에러 해결을 위해 public으로 선언
    public int Capacity => _slots.Count;

    protected InventoryBase(int capacity)
    {
        _slots = new List<InventorySlot>(capacity);
        for (int i = 0; i < capacity; i++) _slots.Add(new InventorySlot());
    }

    protected abstract int GetStackLimit(BaseItem item);

    public virtual void AddItem(BaseItem item, int quantity)
    {
        int limit = GetStackLimit(item);

        // 1. 기존 슬롯에 채우기
        var existing = _slots.FindAll(s => s.item != null && s.item.id == item.id && s.quantity < limit);
        foreach (var slot in existing)
        {
            int addAmount = Mathf.Min(quantity, limit - slot.quantity);
            slot.quantity += addAmount;
            quantity -= addAmount;
            if (quantity <= 0) return;
        }

        // 2. 빈 슬롯에 채우기
        while (quantity > 0)
        {
            var emptySlot = _slots.Find(s => s.IsEmpty);
            if (emptySlot == null) break; 

            emptySlot.item = item;
            int addAmount = Mathf.Min(quantity, limit);
            emptySlot.quantity = addAmount;
            quantity -= addAmount;
        }
    }
    
    public InventorySlot GetSlot(int index) => _slots[index];

    public bool TryRemoveItem(string itemID, int quantity)
    {
        int total = _slots.Where(s => s.item?.id == itemID).Sum(s => s.quantity);
        if (total < quantity) return false;

        var targetSlots = _slots.Where(s => s.item?.id == itemID).OrderBy(s => s.quantity);
        foreach (var slot in targetSlots)
        {
            int removeAmount = Mathf.Min(quantity, slot.quantity);
            slot.quantity -= removeAmount;
            quantity -= removeAmount;
            if (slot.quantity <= 0) slot.Clear();
            if (quantity <= 0) break;
        }
        return true;
    }

    public IReadOnlyDictionary<string, int> GetItemList() => 
        _slots.Where(s => !s.IsEmpty)
              .GroupBy(s => s.item.id)
              .ToDictionary(g => g.Key, g => g.Sum(s => s.quantity));

    public void SortById() => _slots = _slots.OrderBy(s => s.item?.id ?? "ZZZ").ToList();
    public IEnumerable<BaseItem> FilterByCategory(ItemCategory category) => _slots.Where(s => s.item?.category == category).Select(s => s.item).Distinct();
    public IEnumerable<BaseItem> SearchByName(string name) => _slots.Where(s => s.item != null && s.item.itemName.Contains(name)).Select(s => s.item).Distinct();
}