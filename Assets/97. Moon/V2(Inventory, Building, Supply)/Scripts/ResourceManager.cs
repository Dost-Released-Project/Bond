using UnityEngine;

public class ResourceManager
{
    private int _frontierData;
    private int _wood;
    private int _ore;

    // 최대치 설정 (예시값)
    private int MaxFrontierData = 5000;
    private int MaxBuildingResources = 100;

    public int FrontierData => _frontierData;

    public void AddFrontierData(int amount) => _frontierData = Mathf.Min(_frontierData + amount, MaxFrontierData);
    
    public void AddResource(string type, int amount)
    {
        if (type == "Wood") _wood = Mathf.Min(_wood + amount, MaxBuildingResources);
        else if (type == "Ore") _ore = Mathf.Min(_ore + amount, MaxBuildingResources);
    }

    public bool ConsumeFrontierData(int amount)
    {
        if (_frontierData < amount) return false;
        _frontierData -= amount;
        return true;
    }
}