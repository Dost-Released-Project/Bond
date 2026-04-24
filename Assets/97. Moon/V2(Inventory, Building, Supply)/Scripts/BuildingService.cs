using UnityEngine;

public class BuildingService
{
    private readonly ResourceManager _resourceManager;

    public BuildingService(ResourceManager rm)
    {
        _resourceManager = rm;
    }

    public void TryBuild(BuildingData data)
    {
        if (_resourceManager.ConsumeResources(data.frontierCost, data.woodCost, data.oreCost))
        {
            Debug.Log($"<color=green>[건설 완료]</color> {data.buildingName} (Type: {data.buildingType})");
            // 여기서 실제 건물 프리팹 생성 로직 호출
        }
        else
        {
            Debug.LogWarning($"[자원 부족] {data.buildingName} 건설에 필요한 자원이 부족합니다.");
        }
    }
}