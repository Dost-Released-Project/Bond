using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using UnityEngine.AddressableAssets;

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

    public virtual void ClearAll()
    {
        _slots.Clear();
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

    public List<InventorySlot> GetAll()
    {
        return _slots;
    }
    
    public void SortById() { _slots = _slots.OrderBy(s => s.item?.id ?? "ZZZ").ToList(); OnChanged?.Invoke(); }
    public abstract int AddItemAuto(BaseItem item, int quantity);

    public int AddItemId(string id, int quantity)
    {
        if (quantity <= 0) return 0;

        // // 1. 아이템 DB 로드
        BaseItem item = DBSORegistry.GetSO<BaseItem>(id);
        if (item == null)
        {
            Debug.LogWarning($"[인벤토리] DB에서 아이템 ID [{id}]를 찾을 수 없습니다.");
            return quantity;
        }

        // 2. 기획 가이드 반영: 인벤토리 타입 및 카테고리별 분할 여부 결정
        // 이 스스크립트가 붙은 실제 인스턴스 타입 명칭을 확인합니다.
        bool isTotalInventory = this is TotalInventory;
        bool shouldSplit = true;

        if (isTotalInventory)
        {
            // 토탈 인벤토리일 때는 오직 '장신구(Accessories)'만 분할 처리 규칙 적용
            shouldSplit = (item.category == ItemCategory.Accessories);
        }
        // 탐사 인벤토리(ExpeditionInventory)일 때는 필터 없이 소모품/장신구 둘 다 true 유지

        // 분할 처리를 하지 않는 대상이라면 기존처럼 통째로 한 번에 던지고 끝냅니다.
        if (!shouldSplit)
        {
            return AddItemAuto(item, quantity);
        }

        // 3. 한 칸당 최대치 제한 규격 파악
        int slotMax = item.expeditionSlotMax;
        if (slotMax <= 0) slotMax = 1;

        int remainingQuantity = quantity;

        // 4. [치명적 버그 수정] 기존에 이미 가방에 존재하는 같은 아이템의 '잔여 공간' 먼저 채우기
        // 자식 인벤토리가 가진 현재 누적 수량이나 슬롯을 검사하여 기존 자리를 먼저 채워 줍니다.
        // 인벤토리 코어 클래스에 현재 특정 아이템의 총 소지 개수를 가져오는 함수가 있다면 매칭합니다.
        // 없거나 계산이 복잡하다면, 안전하게 1개씩이 아니라 기존 슬롯의 빈 틈을 채우도록 유도합니다.

        int currentItemTotalCount = this.GetItemCount(item.id); // (자식 인벤토리에 구현되어 있는 소지량 체크 함수 호출)
        if (currentItemTotalCount > 0)
        {
            int currentOccupiedSlots = Mathf.CeilToInt((float)currentItemTotalCount / slotMax);
            int totalCapacityOfOccupiedSlots = currentOccupiedSlots * slotMax;
            int currentResidualRoom = totalCapacityOfOccupiedSlots - currentItemTotalCount; // 기존 슬롯의 남은 빈틈

            // 채워넣을 빈 틈이 있다면 그만큼 먼저 채워넣어 증발 예외를 원천 차단!
            if (currentResidualRoom > 0 && remainingQuantity > 0)
            {
                int fillQty = Mathf.Min(currentResidualRoom, remainingQuantity);
                int remainFromFill = AddItemAuto(item, fillQty);

                // 정상적으로 들어간 수량만큼 전체 차감
                remainingQuantity -= (fillQty - remainFromFill);

                // 가방이 완전 풀이라 아예 튕겨나왔다면 즉시 남은 수량 반환 후 종료
                if (remainFromFill >= fillQty) return remainingQuantity;
            }
        }

        // 5. 기존 자리를 다 채우고도 남은 수량이 있다면, 새 슬롯을 열어가며 slotMax 단위로 안전 분할 수납
        while (remainingQuantity > 0)
        {
            int chunkQty = Mathf.Min(slotMax, remainingQuantity);
            int remainFromAuto = AddItemAuto(item, chunkQty);

            // 가방이 더 이상 새 슬롯을 열 공간(Capacity 한도)이 없다면 무한루프 방지 탈출
            if (remainFromAuto >= chunkQty)
            {
                break;
            }

            remainingQuantity -= (chunkQty - remainFromAuto);
        }

        return remainingQuantity;
    }

    public virtual bool ConsumeItem(string itemId, int quantity)
    {
        if (quantity <= 0) return true;

        int totalCount = GetItemCount(itemId);
        if (totalCount < quantity) return false;

        int remainingToRemove = quantity;
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            var slot = _slots[i];
            if (slot != null && !slot.IsEmpty && slot.item != null && slot.item.id == itemId)
            {
                int removeQty = Mathf.Min(slot.quantity, remainingToRemove);
                RemoveFromSlot(i, removeQty); // RemoveFromSlot already calls OnChanged
                remainingToRemove -= removeQty;
                if (remainingToRemove <= 0) break;
            }
        }
        return true;
    }

    public virtual bool ConsumeItemByType(ConsumableType type, int quantity)
    {
        if (quantity <= 0) return true;

        int totalCount = 0;
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
            if (slot != null && !slot.IsEmpty && slot.item is ConsumableItem cItem && cItem.consumableType == type)
            {
                totalCount += slot.quantity;
            }
        }

        if (totalCount < quantity) return false;

        int remainingToRemove = quantity;
        for (int i = _slots.Count - 1; i >= 0; i--)
        {
            var slot = _slots[i];
            if (slot != null && !slot.IsEmpty && slot.item is ConsumableItem cItem && cItem.consumableType == type)
            {
                int removeQty = Mathf.Min(slot.quantity, remainingToRemove);
                RemoveFromSlot(i, removeQty); 
                remainingToRemove -= removeQty;
                if (remainingToRemove <= 0) break;
            }
        }
        return true;
    }

    private int GetItemCount(string itemID)
    {
        if (string.IsNullOrEmpty(itemID)) return 0;
    
        int totalCount = 0;
    
        // 인벤토리 베이스가 들고 있는 전체 슬롯 크기만큼 순회합니다.
        // 만약 내부 슬롯 리스트 명칭이 _slots가 아니라면 기존 변수명에 맞게 매칭해 주세요.
        for (int i = 0; i < _slots.Count; i++)
        {
            var slot = _slots[i];
        
            // 슬롯이 비어있지 않고, 슬롯 내부 아이템의 고유 ID가 일치하는지 검사
            if (slot != null && !slot.IsEmpty && slot.item != null && slot.item.id == itemID)
            {
                totalCount += slot.quantity;
            }
        }
    
        return totalCount;
    }
}