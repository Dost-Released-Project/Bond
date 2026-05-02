using UnityEngine;
using System;
using System.Collections.Generic;

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
    }
}