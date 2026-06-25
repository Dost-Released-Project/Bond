using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bond.Persistence;
using UnityEngine;

public class ExpeditionInventory : InventoryBase, IExpeditionInventory
{
    public ExpeditionInventory(int capacity = 0) : base(capacity) { }

    protected override int GetStackLimit(BaseItem item) => item.expeditionSlotMax;

    // 타운 씬에 들어왔을 때 매니저가 수동으로 호출해 줄 진짜 진입점
    public void InitAndLoad()
    {
        // 1. 기존에 잘못 구독되어 중복 저장되던 이벤트가 있다면 깔끔하게 청소
        OnChanged -= SaveExpeditionInventory;

        // 2. 파일 데이터를 정석대로 읽어와 가방 복구 진행
        LoadExpeditionInventory();

        // 3. 로드가 완벽하게 끝나서 안전해진 이 시점부터만 자동 저장 추적 개시
        OnChanged += SaveExpeditionInventory;
    }

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
    
    public override void ExpandStorage(int additionalSlots)
    {
        base.ExpandStorage(additionalSlots);
        SaveExpeditionInventory(); 
    }
    
    private void LoadExpeditionInventory()
    {
        var save = new InventorySaveData("exp_inv");
        string saveKey = save.Key;
        string path = Path.Combine(Application.dataPath, "Data", "Save", $"{saveKey}.json");

        if (File.Exists(path))
        {
            try 
            {
                SaveLoadSystem.Load(save);
                ClearAll(); 
                
                for (int i = 0; i < save.capacity; i++) _slots.Add(new InventorySlot());

                foreach (var s in save.slots)
                {
                    BaseItem item = DBSORegistry.GetSO<BaseItem>(s.id);
                    if (item != null)
                    {
                        base.AddItemAt(_slots.FindIndex(slot => slot.IsEmpty), item, s.count);
                    }
                }
                Debug.Log($"[ExpeditionInventory] 타운 씬 시점 파일 로드 완공. (용량: {Capacity})");
            }
            catch (Exception e)
            {
                Debug.LogError($"[ExpeditionInventory] 로드 중 오류 발생 - {e.Message}");
            }
        }
        else
        {
            // 새로하기 상황: 파일이 없으면 얄짤없이 4칸 고정 후 즉시 세이브 파일을 새겨서 락을 굽습니다.
            ClearAll();
            for (int i = 0; i < 4; i++) _slots.Add(new InventorySlot());
            Debug.Log("[ExpeditionInventory] 세이브 파일이 없으므로 정석 4칸으로 롤백 정산합니다.");
            
            // 파일이 아예 없어서 새로 만들어야 할 때만 물리 파일을 디스크에 강제 주입
            SaveExpeditionInventoryDirect();
        }
    }
    
    public void SaveExpeditionInventory()
    {
        // 타이틀 화면이나 다른 곳에서 꼬여서 파일이 지워졌을 때 뜬금없이 파일이 다시 구워지는 현상 전면 차단
        string path = Path.Combine(Application.dataPath, "Data", "Save", "exp_inv.json");
        
        // 만약 새로하기로 세이브를 밀어버린 상태에서 수동 로드(InitAndLoad)를 안 거쳤다면 저장을 거부
        if (!File.Exists(path) && Capacity > 2) 
        {
            Debug.LogWarning("[ExpeditionInventory] 정식 로드가 진행되지 않은 상태에서의 유령 저장을 차단합니다.");
            return;
        }

        SaveExpeditionInventoryDirect();
    }

    // 순수하게 현재 가방 상태를 디스크에 강제 인쇄하는 로직
    private void SaveExpeditionInventoryDirect()
    {
        var save = new InventorySaveData("exp_inv");
        save.capacity = Capacity; 
        foreach (var slot in GetAll())
        {
            if (!slot.IsEmpty)
                save.slots.Add(new InventorySaveData.SlotData { id = slot.item.id, count = slot.quantity });
        }
        SaveLoadSystem.Save(save);
    }
}