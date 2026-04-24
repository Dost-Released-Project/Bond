using UnityEngine;

public class BuildingService
{
    private readonly ResourceManager _resourceManager;

    public BuildingService(ResourceManager rm)
    {
        _resourceManager = rm;
    }

    // 건설은 무조건 1레벨 데이터를 참조
    public bool TryBuild(BuildingData data)
    {
        var level1 = data.GetLevelData(1);
        
        if (_resourceManager.ConsumeResources(level1.frontierCost, level1.woodCost, level1.oreCost))
        {
            Debug.Log($"<color=green>[건설 성공]</color> {data.buildingName}이 건설되었습니다.");
            return true;
        }

        Debug.LogWarning($"[자원 부족] {data.buildingName} 건설 불가.");
        return false;
    }

    // 업그레이드는 시도하려는 다음 레벨 데이터를 참조
    public bool TryUpgrade(BuildingData data, int targetLevel)
    {
        var targetData = data.GetLevelData(targetLevel);

        if (_resourceManager.ConsumeResources(targetData.frontierCost, targetData.woodCost, targetData.oreCost))
        {
            Debug.Log($"<color=cyan>[업그레이드 성공]</color> {data.buildingName} Lv.{targetLevel}");
            return true;
        }

        Debug.LogWarning($"[자원 부족] {data.buildingName} Lv.{targetLevel} 업그레이드 불가.");
        return false;
    }
}