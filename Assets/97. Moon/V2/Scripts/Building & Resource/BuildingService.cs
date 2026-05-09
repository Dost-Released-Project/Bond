using UnityEngine;

public class BuildingService
{
    private readonly ResourceManager _resourceManager;

    public BuildingService(ResourceManager rm) { _resourceManager = rm; }

    public bool TryBuild(BuildingData data)
    {
        var level1 = data.GetLevelData(1);
        bool canAfford = _resourceManager.ConsumeResources(level1.frontierCost, level1.woodCost, level1.oreCost);
    
        if (canAfford)
        {
            Debug.Log($"<color=green>[Service]</color> {data.DisplayName} 건설 자원 소모 완료.");
        }
        else
        {
            Debug.LogWarning($"<color=red>[Service]</color> 자원 부족: {data.DisplayName} (필요 개척데이터: {level1.frontierCost})");
        }
    
        return canAfford;
    }

    public bool TryUpgrade(BuildingData data, int targetLevel)
    {
        var targetData = data.GetLevelData(targetLevel);
        bool canAfford = _resourceManager.ConsumeResources(targetData.frontierCost, targetData.woodCost, targetData.oreCost);

        if (canAfford)
        {
            Debug.Log($"<color=cyan>[Service]</color> {data.DisplayName} Lv.{targetLevel} 업그레이드 자원 소모 완료.");
        }
        else
        {
            Debug.LogWarning($"<color=red>[Service]</color> 업그레이드 실패: {data.DisplayName} Lv.{targetLevel} 자원 부족.");
        }

        return canAfford;
    }

    // SettlementManager에서 이관된 캐릭터 상호작용 로직
    public void ExecuteTavern(BaseCharacter target, BuildingLevelData data)
    {
        if (target == null) return;
        target.RecoverHp(data.effectValue);
        Debug.Log($"[BuildingService] 식당 이용: {data.effectValue} HP 회복");
    }

    public void ExecuteInn(BaseCharacter target, BuildingLevelData data)
    {
        if (target == null) return;
        target.RecoverInsanity(data.effectValue);
        Debug.Log($"[BuildingService] 여관 이용: {data.effectValue} 광기 회복");
    }

    public void UpgradeEquipment(Stat targetStat, Equipment equipment, int smithyLevel)
    {
        if (equipment == null || equipment.upgradeLevel >= smithyLevel || equipment.upgradeLevel >= 5) return;

        int costFrontier = (equipment.upgradeLevel + 1) * 100;
        int costOre = (equipment.upgradeLevel + 1) * 10;

        if (_resourceManager.ConsumeResources(costFrontier, 0, costOre))
        {
            equipment.Upgrade();
            targetStat.StatCalculate();
        }
    }
}