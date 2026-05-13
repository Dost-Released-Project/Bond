using System;
using System.Collections.Generic;
using Bond.Persistence;
using Newtonsoft.Json;
using UnityEngine;

// 건물 데이터
public class SettlementSaveData : ISaveable<SettlementSaveData>
{
    public string Key => "settlement_data";
    public List<BuildingSaveInfo> buildings = new();

    public struct BuildingSaveInfo { public int slotIndex; public BuildingType type; public int level; }

    [JsonIgnore]
    public SettlementSaveData Data => this;
    public void Restore(SettlementSaveData data) => this.buildings = data.buildings;
}