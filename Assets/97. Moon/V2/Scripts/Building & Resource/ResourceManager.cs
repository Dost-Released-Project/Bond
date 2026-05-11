using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Bond.Persistence;

public enum ResourceType { Frontier, Wood, Ore }

public class ResourceManager
{
    private Dictionary<ResourceType, ResourceData> _resources = new();
    public event Action<ResourceType, ResourceData> OnResourceChanged;

    public ResourceManager()
    {
        _resources[ResourceType.Frontier] = new ResourceData("개척 데이터", 1000);
        _resources[ResourceType.Wood] = new ResourceData("목재", 100);
        _resources[ResourceType.Ore] = new ResourceData("광석", 100);
        
        // 로드 시도
        var loadData = new ResourceSaveData();
        // SaveLoadSystem의 GetPath와 Key를 조합하여 경로 생성 (시스템 수정 없이 대응)
        string saveKey = loadData.Key;
        string path = Path.Combine(Application.dataPath, "Data", "Save", $"{saveKey}.json");
        

        if (File.Exists(path))
        {
            try 
            {
                SaveLoadSystem.Load(loadData);
                
                _resources[ResourceType.Frontier].Max = loadData.frontierMax;
                _resources[ResourceType.Wood].Max = loadData.woodMax;
                _resources[ResourceType.Ore].Max = loadData.oreMax;
            
                _resources[ResourceType.Frontier].Current = loadData.frontierCur;
                _resources[ResourceType.Wood].Current = loadData.woodCur;
                _resources[ResourceType.Ore].Current = loadData.oreCur;
                Debug.Log("ResourceManager: 데이터 로드 성공");
            }
            catch (Exception e)
            {
                Debug.LogError($"ResourceManager: 로드 중 오류 발생 - {e.Message}");
            }
        }
        else 
        {
            Debug.Log("ResourceManager: 기존 세이브 없음. 기본값으로 시작.");
        }
    }

    // 기존 프로퍼티 유지 (호환성)
    public int FrontierData => _resources[ResourceType.Frontier].Current;
    public int Wood => _resources[ResourceType.Wood].Current;
    public int Ore => _resources[ResourceType.Ore].Current;

    public void ExpandCapacities(int fAdd, int wAdd, int oAdd)
    {
        _resources[ResourceType.Frontier].Max += fAdd;
        _resources[ResourceType.Wood].Max += wAdd;
        _resources[ResourceType.Ore].Max += oAdd;
        NotifyAll();
    }

    public void AddResource(ResourceType type, int amount)
    {
        var res = _resources[type];
        res.Current = Mathf.Clamp(res.Current + amount, 0, res.Max);
        OnResourceChanged?.Invoke(type, res);
        NotifyAll();
    }

    // 기존 메서드 보존 (기능 유지)
    public void AddFrontierData(int amount) => AddResource(ResourceType.Frontier, amount);
    public void AddMaterials(int wood, int ore) { AddResource(ResourceType.Wood, wood); AddResource(ResourceType.Ore, ore); }
    
    public void Admin_AddAllResources(int amount)
    {
        foreach (var type in _resources.Keys) AddResource(type, amount);
    }

    public bool ConsumeResources(int frontier, int wood, int ore)
    {
        if (FrontierData >= frontier && Wood >= wood && Ore >= ore)
        {
            _resources[ResourceType.Frontier].Current -= frontier;
            _resources[ResourceType.Wood].Current -= wood;
            _resources[ResourceType.Ore].Current -= ore;
            NotifyAll();
            return true;
        }
        return false;
    }

    private void NotifyAll()
    {
        foreach (var res in _resources) OnResourceChanged?.Invoke(res.Key, res.Value);
        
        // 세이브 추가
        var resSave = new ResourceSaveData {
            frontierCur = FrontierData, woodCur = Wood, oreCur = Ore,
            frontierMax = _resources[ResourceType.Frontier].Max, woodMax = _resources[ResourceType.Wood].Max, oreMax = _resources[ResourceType.Ore].Max
        };
        SaveLoadSystem.Save(resSave);
    }
}