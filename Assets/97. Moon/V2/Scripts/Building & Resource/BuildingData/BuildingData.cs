using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;

[System.Serializable]
public struct BuildingLevelData
{
    public int level;
    public int frontierCost;
    public int woodCost;
    public int oreCost;
    
    [Header("건물 성능 (예: 인벤토리 슬롯 수, 회복량 등)")]
    public int effectValue; // 수치 
    public int slotExpansion; // 창고 확장
    public int frontierCapAdd; // 개척 데이터 최대치 증가량
    public int materialCapAdd; // 목재/광물 최대치 증가량
    public int maxUses;
}

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Settle/Building Data")]
public class BuildingData : BaseSO
{
    public BuildingType buildingType;
    public Sprite buildingSprite;
    public List<BuildingLevelData> levels = new();

    // DTO를 받지 않고, 낱개 파라미터를 받도록 수정 (Consumable 방식)
    public void SetBuildingData(string id, string name, string desc, BuildingType type, List<BuildingLevelData> levelList, Sprite sprite)
    {
        base.Initialize(id, name, desc);
        this.buildingType = type;
        this.levels = levelList;
        this.buildingSprite = sprite;
    }

    public BuildingLevelData GetLevelData(int level)
    {
        int index = Mathf.Clamp(level - 1, 0, levels.Count - 1);
        return levels[index];
    }
    
    // 주소를 받아 아이콘을 비동기로 로드하는 함수
    public async Task LoadIconAsync(string path)
    {
        if (string.IsNullOrEmpty(path)) return;

        var handle = Addressables.LoadAssetAsync<Sprite>(path);
        buildingSprite = await handle.Task;

        if (handle.Status != UnityEngine.ResourceManagement.AsyncOperations.AsyncOperationStatus.Succeeded)
        {
            Debug.LogError($"{Id}의 아이콘 로드 실패. 경로: {path}");
        }
    }
}