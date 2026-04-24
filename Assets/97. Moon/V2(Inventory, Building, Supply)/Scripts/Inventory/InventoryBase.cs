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

    public abstract int GetStackLimit(BaseItem item);

    // [요구사항 5] 특정 인덱스에 아이템 배치
    public virtual int AddItemAt(int index, BaseItem item, int quantity)
    {
        var slot = _slots[index];
        int limit = GetStackLimit(item);

        if (slot.IsEmpty)
        {
            slot.item = item;
            int add = Mathf.Min(quantity, limit);
            slot.quantity = add;
            return quantity - add;
        }
        else if (slot.item.id == item.id)
        {
            int canAdd = limit - slot.quantity;
            int add = Mathf.Min(quantity, canAdd);
            slot.quantity += add;
            return quantity - add;
        }
        return quantity; // 빈 공간 없으면 그대로 반환
    }

    // [요구사항 4] 특정 슬롯에서만 정확히 제거
    public void RemoveFromSlot(int index, int quantity)
    {
        var slot = _slots[index];
        slot.quantity -= quantity;
        if (slot.quantity <= 0) slot.Clear();
    }

    // [요구사항 1] 검색 기능 구현
    public IEnumerable<int> GetFilteredIndices(string searchField, ItemCategory? category)
    {
        List<int> result = new();
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot.IsEmpty) { result.Add(i); continue; }

            // 검색어 포함 여부 (대소문자 무시)
            bool matchSearch = string.IsNullOrEmpty(searchField) || 
                               slot.item.itemName.ToLower().Contains(searchField.ToLower());
            // 카테고리 일치 여부
            bool matchCategory = !category.HasValue || slot.item.category == category.Value;

            if (matchSearch && matchCategory) result.Add(i);
        }
        return result;
    }
    
    public void ExpandStorage(int additionalSlots)
    {
        for (int i = 0; i < additionalSlots; i++)
        {
            _slots.Add(new InventorySlot());
        }
        Debug.Log($"슬롯 확장: {additionalSlots}개 추가됨, 전체 슬롯 {_slots.Count} 개");
    }

    public abstract int AddItemAuto(BaseItem item, int quantity);
    public void ClearSlot(int index) => _slots[index].Clear();
    public InventorySlot GetSlot(int index) => _slots[index];

    public void SortById() => _slots = _slots.OrderBy(s => s.item?.id ?? "ZZZ").ToList();
}
