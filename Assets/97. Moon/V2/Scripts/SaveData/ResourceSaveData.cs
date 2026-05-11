using System;
using System.Collections.Generic;
using Bond.Persistence;
using Newtonsoft.Json;
using UnityEngine;

// 리소스 데이터
public class ResourceSaveData : ISaveable<ResourceSaveData>
{
    public string Key => "resource_data";
    public int frontierCur, frontierMax;
    public int woodCur, woodMax;
    public int oreCur, oreMax;

    [JsonIgnore]
    public ResourceSaveData Data => this;
    public void Restore(ResourceSaveData data)
    {
        this.frontierCur = data.frontierCur; this.frontierMax = data.frontierMax;
        this.woodCur = data.woodCur; this.woodMax = data.woodMax;
        this.oreCur = data.oreCur; this.oreMax = data.oreMax;
    }
}
