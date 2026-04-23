using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class InventoryBase : IInventory
{
    protected List<InventorySlot> _slots;
    public int Capacity => _slots.Count;

    protected InventoryBase(int capacity)
    {
        _slots = new List<InventorySlot>(capacity);
        for (int i = 0; i < capacity; i++) _slots.Add(new InventorySlot());
    }
    
    public void SwapSlots(int indexA, int indexB)
    {
        // 리스트 내의 두 요소 위치를 바꿈
        var temp = _slots[indexA];
        _slots[indexA] = _slots[indexB];
        _slots[indexB] = temp;
    }

    public abstract int GetStackLimit(BaseItem item);
    public InventorySlot GetSlot(int index) => _slots[index];

    // [요구사항 3 반영] 아이템 추가 로직 개선 (Total은 중복 방지)
    public virtual int AddItem(BaseItem item, int quantity)
    {
        int limit = GetStackLimit(item);
        int remaining = quantity;

        // 1. 기존에 해당 아이템이 있는 슬롯 찾기
        var existingSlots = _slots.Where(s => s.item?.id == item.id && s.quantity < limit);
        foreach (var slot in existingSlots)
        {
            int addAmount = Mathf.Min(remaining, limit - slot.quantity);
            slot.quantity += addAmount;
            remaining -= addAmount;
            if (remaining <= 0) return 0;
        }

        // 2. 빈 슬롯에 채우기 (TotalInventory는 상속받은 곳에서 체크)
        while (remaining > 0)
        {
            var emptySlot = _slots.Find(s => s.IsEmpty);
            if (emptySlot == null) break; 

            emptySlot.item = item;
            int addAmount = Mathf.Min(remaining, limit);
            emptySlot.quantity = addAmount;
            remaining -= addAmount;
        }
        return remaining; // 못 담고 남은 개수 반환
    }

    public bool TryRemoveItem(string itemID, int quantity)
    {
        int total = _slots.Where(s => s.item?.id == itemID).Sum(s => s.quantity);
        if (total < quantity) return false;

        int toRemove = quantity;
        var targetSlots = _slots.Where(s => s.item?.id == itemID).OrderByDescending(s => s.quantity);
        foreach (var slot in targetSlots)
        {
            int removeAmount = Mathf.Min(toRemove, slot.quantity);
            slot.quantity -= removeAmount;
            toRemove -= removeAmount;
            if (slot.quantity <= 0) slot.Clear();
            if (toRemove <= 0) break;
        }
        return true;
    }

    // [요구사항 5 반영] 검색 및 필터 기능 구현
    public void SortById() => _slots = _slots.OrderBy(s => s.item?.id ?? "ZZZ").ToList();
    public IEnumerable<BaseItem> FilterByCategory(ItemCategory category) => _slots.Where(s => s.item?.category == category).Select(s => s.item).Distinct();
    public IEnumerable<BaseItem> SearchByName(string name) => _slots.Where(s => s.item != null && s.item.itemName.Contains(name)).Select(s => s.item).Distinct();
    public IReadOnlyDictionary<string, int> GetItemList() => _slots.Where(s => !s.IsEmpty).GroupBy(s => s.item.id).ToDictionary(g => g.Key, g => g.Sum(s => s.quantity));
}