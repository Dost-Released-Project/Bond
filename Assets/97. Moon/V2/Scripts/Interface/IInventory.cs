using System;
using UnityEngine;
using System.Collections.Generic;
using System.Threading.Tasks;

public interface IInventory
{
    // --- 기본 관리 (인덱스 기반) ---
    int AddItemAt(int index, BaseItem item, int quantity); // 특정 슬롯에 추가
    int AddItemAuto(BaseItem item, int quantity);          // 자동 추가 (우클릭 등)
    int AddItemId(string id, int quantity);          // 아이템 ID로 추가
    void RemoveFromSlot(int index, int quantity);         // 특정 슬롯에서 제거
    void ClearSlot(int index);
    
    void ClearAll();

    List<InventorySlot> GetAll();
    
    // --- 아이템 소모 ---
    bool ConsumeItem(string itemId, int quantity);
    bool ConsumeItemByType(ConsumableType type, int quantity);
    
    InventorySlot GetSlot(int index);
    int Capacity { get; }

    // --- 검색 및 정렬 ---
    void SortById();
    IEnumerable<int> GetFilteredIndices(string searchField, ItemCategory? category);
    
    void ExpandStorage(int additionalSlots);
    
    event Action OnChanged; // 데이터 변경 알림 이벤트
}