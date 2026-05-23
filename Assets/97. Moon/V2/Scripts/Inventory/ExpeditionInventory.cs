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