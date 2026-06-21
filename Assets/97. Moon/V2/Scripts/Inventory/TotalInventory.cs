using System;
using System.IO;
using System.Threading.Tasks;
using Bond.Persistence;
using UnityEngine;
using UnityEngine.AddressableAssets;

public class TotalInventory : InventoryBase, ITotalInventory
{
    private readonly ResourceManager _resourceManager;
    public TotalInventory(int capacity, ResourceManager rm) : base(capacity) { _resourceManager = rm; }

    protected override int GetStackLimit(BaseItem item) => item.totalGlobalMax;
    
    public override int AddItemAuto(BaseItem item, int quantity)
    {
        if (item == null) return quantity;

        // 장신구가 아닐 때만 기존 스택을 찾음 (장신구는 중복/개별 슬롯 허용)
        if (item.category != ItemCategory.Accessories)
        {
            int existingIdx = _slots.FindIndex(s => !s.IsEmpty && s.item.id == item.id && s.quantity < GetStackLimit(item));
            if (existingIdx != -1) return AddItemAt(existingIdx, item, quantity);
        }
    
        // 빈 슬롯 찾기
        int emptyIdx = _slots.FindIndex(s => s.IsEmpty);
        if (emptyIdx != -1) return AddItemAt(emptyIdx, item, quantity);
    
        return quantity;
    }

    public override int AddItemAt(int index, BaseItem item, int quantity)
    {
        // [수정] 장신구가 아닐 때만 '다른 슬롯'의 동일 아이템과 합치기 수행
        if (item.category != ItemCategory.Accessories)
        {
            int existingIdx = _slots.FindIndex(s => !s.IsEmpty && s.item.id == item.id);
            if (existingIdx != -1 && existingIdx != index) index = existingIdx;
        }
    
        int limit = GetStackLimit(item);
        var slot = _slots[index];

        if (slot.IsEmpty) { slot.item = item; slot.quantity = 0; }

        int canAdd = limit - slot.quantity;
        int actualAdd = Mathf.Min(quantity, canAdd);
        int excess = quantity - actualAdd;

        slot.quantity += actualAdd;
        if (excess > 0) ProcessExcessToLog(item, excess);
    
        NotifyChanged();
        return 0;
    }
    
    public void ProcessExcessToLog(BaseItem item, int quantity)
    {
        _resourceManager.AddFrontierData(quantity * 5);
        Debug.Log($"<color=cyan>[로그]</color> {item.itemName} 초과분 {quantity}개 데이터 전환.");
    }
    
    public override void ExpandStorage(int additionalSlots)
    {
        base.ExpandStorage(additionalSlots);
        SaveTotalInventory(); 
    }
    
    public void LoadTotalInventory()
    {
        // 로드 시도
        var save = new InventorySaveData("total_inv");
        // SaveLoadSystem의 GetPath와 Key를 조합하여 경로 생성 (시스템 수정 없이 대응)
        string saveKey = save.Key;
        string path = Path.Combine(Application.dataPath, "Data", "Save", $"{saveKey}.json");

        if (File.Exists(path))
        {
            try 
            {
                SaveLoadSystem.Load(save);
                
                // 1. 기존 슬롯을 완전히 비우고 저장된 용량만큼 재생성
                ClearAll(); 
                ExpandStorage(save.capacity);

                // 2. 아이템 복구
                foreach (var s in save.slots)
                {
                    // 아이템 데이터 베이스 접근
                    BaseItem item = DBSORegistry.GetSO<BaseItem>(s.id);
                    if (item != null) AddItemAuto(item, s.count);
                }
                
                Debug.Log("TotalInventory: 데이터 로드 성공");
            }
            catch (Exception e)
            {
                Debug.LogError($"TotalInventory: 로드 중 오류 발생 - {e.Message}");
            }
        }
        else
        {
            Debug.Log("TotalInventory: 기존 세이브 없음. 기본값으로 시작.");
        }
    }
    
    public void SaveTotalInventory()
    {
        // 1. 저장할 데이터 객체 생성 (파일명: total_inv)
        var save = new InventorySaveData("total_inv");
        save.capacity = Capacity;
        foreach (var slot in GetAll())
        {
            if (!slot.IsEmpty)
                save.slots.Add(new InventorySaveData.SlotData { id = slot.item.id, count = slot.quantity });
        }

        // 3. 세이브 시스템 실행
        SaveLoadSystem.Save(save);
    }
}