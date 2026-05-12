using System;
using System.Collections.Generic;
using Bond.Persistence;
using Newtonsoft.Json;
using UnityEngine;

// 인벤토리 데이터 (전체/탐사 공용)
public class InventorySaveData : ISaveable<InventorySaveData>
{
    private string _key;
    public string Key => _key;
    public List<SlotData> slots = new();

    public struct SlotData { public string id; public int count; public int capacity; }

    public InventorySaveData(string key) => _key = key; // "total_inv" 또는 "exp_inv"

    [JsonIgnore]
    public InventorySaveData Data => this;
    public void Restore(InventorySaveData data) => this.slots = data.slots;
}