using System;
using System.Collections.Generic;
using Bond.Persistence;
using Newtonsoft.Json;
using UnityEngine;

// 인벤토리 데이터 (전체/탐사 공용)
[Serializable]
public class InventorySaveData : ISaveable<InventorySaveData>
{
    private string _key;
    public string Key => _key;
    
    public int capacity; // 용량 필드 추가
    public List<SlotData> slots = new();

    [Serializable] public struct SlotData { public string id; public int count; }

    public InventorySaveData(string key) => _key = key;
    
    [JsonIgnore]
    public InventorySaveData Data => this;
    public void Restore(InventorySaveData data)
    {
        this.capacity = data.capacity;
        this.slots = data.slots;
    }
}