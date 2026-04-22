using UnityEngine;
using System.Collections.Generic;

public interface IInventory
{
    // --- 기본 관리 ---
    int Capacity { get; } // VContainer에서 접근 가능하도록 추가
    void AddItem(BaseItem item, int quantity);
    bool TryRemoveItem(string itemID, int quantity);
    IReadOnlyDictionary<string, int> GetItemList();
    InventorySlot GetSlot(int index); // 사용자님이 옮긴 위치 확인

    // --- 편의 기능 (신규) ---
    /// <summary>ID 기반 자동 정렬</summary>
    void SortById();

    /// <summary>카테고리별(소모품, 재료, 장신구 등) 아이템 필터링 결과 반환</summary>
    IEnumerable<BaseItem> FilterByCategory(ItemCategory category);

    /// <summary>이름 검색을 통한 아이템 탐색</summary>
    IEnumerable<BaseItem> SearchByName(string name);
}