using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public abstract class InventoryBase : IInventory
{
    protected List<InventorySlot> _slots = new();
    public int Capacity => _slots.Count;
    public event Action OnChanged;

    protected InventoryBase(int capacity)
    {
        for (int i = 0; i < capacity; i++) _slots.Add(new InventorySlot());
    }

    protected abstract int GetStackLimit(BaseItem item);

    public virtual int AddItemAt(int index, BaseItem item, int quantity)
    {
        if (index < 0 || index >= _slots.Count) return quantity;
        
        var slot = _slots[index];
        int limit = GetStackLimit(item);

        if (slot.IsEmpty)
        {
            slot.item = item;
            int add = Mathf.Min(quantity, limit);
            slot.quantity = add;
            OnChanged?.Invoke();
            return quantity - add;
        }
        else if (slot.item.id == item.id)
        {
            int canAdd = limit - slot.quantity;
            int add = Mathf.Min(quantity, canAdd);
            slot.quantity += add;
            OnChanged?.Invoke();
            return quantity - add;
        }
        return quantity;
    }

    public void RemoveFromSlot(int index, int quantity)
    {
        if (index < 0 || index >= _slots.Count) return;
        var slot = _slots[index];
        slot.quantity -= quantity;
        if (slot.quantity <= 0) slot.Clear();
        OnChanged?.Invoke();
    }

    public void ClearSlot(int index)
    {
        _slots[index].Clear();
        OnChanged?.Invoke();
    }

    public virtual void ExpandStorage(int additionalSlots)
    { 
        for (int i = 0; i < additionalSlots; i++) _slots.Add(new InventorySlot());
        OnChanged?.Invoke();
    }

    public IEnumerable<int> GetFilteredIndices(string searchField, ItemCategory? category)
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            // 탐사 인벤토리는 빈 칸도 항상 보여줌
            if (slot.IsEmpty && GetType() == typeof(ExpeditionInventory)) { yield return i; continue; }
            // 필터가 없을 때 빈 칸은 전체 보기에서만 포함
            if (slot.IsEmpty && !category.HasValue) { yield return i; continue; }
            if (slot.IsEmpty) continue;

            bool matchSearch = string.IsNullOrEmpty(searchField) || 
                               slot.item.itemName.ToLower().Contains(searchField.ToLower());
            bool matchCategory = !category.HasValue || slot.item.category == category.Value;

            if (matchSearch && matchCategory) yield return i;
        }
    }
    
    // 자식 클래스에서 호출할 수 있는 "이벤트 발생용" 메서드
    protected void NotifyChanged() {
        OnChanged?.Invoke();
    }

    public InventorySlot GetSlot(int index) => _slots[index];
    public void SortById() { _slots = _slots.OrderBy(s => s.item?.id ?? "ZZZ").ToList(); OnChanged?.Invoke(); }
    public abstract int AddItemAuto(BaseItem item, int quantity);
}



