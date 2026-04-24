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
    
    public void UpgradeEquipment(Stat targetStat, Equipment equipment)
    {
        if (equipment.type != EquipmentType.Base) return; // 기본 장비만 가능
        if (equipment.upgradeLevel >= 5) return;

        // 강화 비용 체크 (예: 레벨당 개척 데이터 100)
        int cost = (equipment.upgradeLevel + 1) * 100;
    
        if (_resourceManager.ConsumeResources(cost, 0, 10)) // 데이터와 광물 소모
        {
            equipment.Upgrade();
        
            // 중요: 장비 강화 후 캐릭터의 스탯을 다시 계산합니다.
            // Stat 클래스에서 장비 스탯을 합산하도록 StatCalculate를 호출해야 함
            targetStat.StatCalculate(); 
            Debug.Log($"{equipment.itemName} 강화 성공! 현재 레벨: {equipment.upgradeLevel}");
        }
    }
}