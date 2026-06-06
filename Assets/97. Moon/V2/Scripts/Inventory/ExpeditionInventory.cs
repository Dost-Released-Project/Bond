using System;
using System.Collections.Generic;
using UnityEngine;


public class ExpeditionInventory : InventoryBase, IExpeditionInventory
{
    // 💥 [신규] 탐사 중 획득한 임시 자원 수량 저장소
    public int AccumulatedFrontier { get; private set; }
    public int AccumulatedWood { get; private set; }
    public int AccumulatedOre { get; private set; }
    
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
    
    // 💥 [신규] 탐사 중 자원을 안전하게 누적하는 인터페이스 메서드
    public void AddAccumulatedResource(int frontier = 0, int wood = 0, int ore = 0)
    { 
        AccumulatedFrontier += frontier;
        AccumulatedWood += wood;
        AccumulatedOre += ore;
    }

    // 💥 [신규] 마을 정산 완료 후 누적 수량을 완전 공백(0) 상태로 복구하는 청소 포트
    public void ClearAccumulatedResources()
    {
        AccumulatedFrontier = 0;
        AccumulatedWood = 0;
        AccumulatedOre = 0;
    }
    
    /// <summary>
    /// 세이브 파일로부터 인벤토리의 저장된 용량만 기습적으로 확인합니다. (파일이 없으면 기본값 반환)
    /// </summary>
    public static int PeekInventoryCapacity(string saveName, int defaultCapacity = 2)
    {
        try
        {
            // 기존 세이브 시스템이 사용하는 경로 규칙과 완전히 통일합니다.
            string path = System.IO.Path.Combine(Application.dataPath, "Data", "Save", $"{saveName}.json");

            if (System.IO.File.Exists(path))
            {
                string json = System.IO.File.ReadAllText(path);
                var rawSave = JsonUtility.FromJson<InventorySaveData>(json);
                return rawSave.capacity < defaultCapacity ? defaultCapacity : rawSave.capacity;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[SaveLoadSystem] 용량 미리보기 중 예외 발생: {e.Message}");
        }

        return defaultCapacity;
    }
}