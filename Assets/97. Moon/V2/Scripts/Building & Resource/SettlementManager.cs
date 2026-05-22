using System;
using System.Linq;
using Bond.Persistence;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class SettlementManager : MonoBehaviour, ISettlementManager
{
    private Transform[] constructionSlots;
    
    private BuildingService _buildingService;
    private ResourceManager _resourceManager;
    private InventoryView _inventoryView;
    private SupplyView _supplyView;
    private ITotalInventory _totalInv;
    private IExpeditionInventory _expeditionInv;
    
    [Inject] private SmithyUIController _smithyUI; 
    [Inject] private CharacterSelector _characterSelector;

    [Inject]
    public void Construct(BuildingService bs, ResourceManager rm, InventoryView iv, SupplyView supply, ITotalInventory total, IExpeditionInventory exp)
    {
        _buildingService = bs; _resourceManager = rm; _inventoryView = iv;
        _supplyView = supply; _totalInv = total; _expeditionInv = exp;
    }
    
    private void Awake()
    {
        constructionSlots = GameObject.FindGameObjectsWithTag("ConstructionSlot")
            .OrderBy(go => go.name)
            .Select(go => go.transform)
            .ToArray();

        Debug.Log($"<color=cyan>[시스템]</color> {constructionSlots.Length}개의 건설 슬롯이 자동으로 등록되었습니다.");
    }
    
    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame) _resourceManager.Admin_AddAllResources(1000);
    }
    
    public void OnBuildingClicked(BuildingObject building)
    {
        var levelData = building.Data.GetLevelData(building.CurrentLevel);
        
        switch (building.Data.buildingType)
        {
            case BuildingType.Storage: _inventoryView.ToggleWindow(true); break;
            case BuildingType.Supply: _supplyView.Open(); break;
            case BuildingType.Tavern: 
                if (_characterSelector.Selected != null)
                    _buildingService.ExecuteTavern(_characterSelector.Selected, levelData); break;
            case BuildingType.Inn: 
                if (_characterSelector.Selected != null)
                    _buildingService.ExecuteInn(_characterSelector.Selected, levelData); break;
            case BuildingType.Smithy: 
                if (_characterSelector.Selected != null)
                    _smithyUI.Open(_characterSelector.Selected, building.CurrentLevel); break;
            case BuildingType.Guild: CollectGuildData(building); break;
        }
    }

    public void BuildInSlot(int slotIndex, BuildingData data)
    {
        Debug.Log($"<color=white>[시스템]</color> 슬롯 {slotIndex}에 {data.DisplayName} 건설 시도 중...");

        if (slotIndex < 0 || slotIndex >= constructionSlots.Length)
        {
            Debug.LogError($"[건설 실패] 유효하지 않은 슬롯 인덱스: {slotIndex}");
            return;
        }

        Transform slotTransform = constructionSlots[slotIndex];

        if (slotTransform.childCount > 0)
        {
            Debug.LogWarning($"[건설 실패] 슬롯 {slotIndex}에 이미 건물이 존재합니다.");
            return;
        }

        if (_buildingService.TryBuild(data))
        {
            Debug.Log($"<color=green>[건설 성공]</color> {data.DisplayName} 건설을 시작합니다.");

            if (slotTransform.TryGetComponent<SpriteRenderer>(out var mr)) mr.enabled = false;
            if (slotTransform.TryGetComponent<BoxCollider>(out var bc)) bc.enabled = false;
            if (slotTransform.TryGetComponent<ConstructionSlot>(out var slotScript)) slotScript.enabled = false;

            CreateBuildingVisual(slotTransform, data);

            // 최초 건설 시에만 기분 좋게 한 번 통통 튑니다.
            var bObj = slotTransform.GetComponentInChildren<BuildingObject>();
            if (bObj != null)
            {
                bObj.TriggerConstructionPopping();
            }

            // 최초 배치 성공 시에만 버프 적용
            ApplyBuildingEffect(data, 1);
            SaveSettlement();
            Debug.Log($"<color=cyan>[시스템]</color> {data.DisplayName} 배치가 완료되었습니다.");
        }
        else
        {
            Debug.LogWarning($"[자원 부족] {data.DisplayName}을 건설하기 위한 자원이 모자랍니다.");
        }
    }

    public void UpgradeBuilding(BuildingObject building)
    {
        int nextLevel = building.CurrentLevel + 1;
        if (nextLevel > building.Data.levels.Count) return;
    
        if (_buildingService.TryUpgrade(building.Data, nextLevel))
        {
            building.Upgrade();
            ApplyBuildingEffect(building.Data, nextLevel);
            SaveSettlement();
        }
    }

    private void ApplyBuildingEffect(BuildingData data, int level)
    {
        var levelData = data.GetLevelData(level);
        switch (data.buildingType)
        {
            case BuildingType.Storage:
                _totalInv.ExpandStorage(levelData.slotExpansion);
                _resourceManager.ExpandCapacities(levelData.frontierCapAdd, levelData.materialCapAdd, levelData.materialCapAdd);
                break;
            case BuildingType.Carriage:
                _expeditionInv.ExpandStorage(levelData.slotExpansion);
                _inventoryView.RefreshUI();
                break;
        }
    }
    
    private void CollectGuildData(BuildingObject guild)
    {
        int reward = guild.Data.GetLevelData(guild.CurrentLevel).effectValue;
        _resourceManager.AddFrontierData(reward);
        _resourceManager.AddMaterials((int)(reward*0.05f), (int)(reward*0.05f));
        Debug.Log($"길드에서 {reward}의 개척 데이터를 수급했습니다!");
    }
    
    private void CreateBuildingVisual(Transform parent, BuildingData data)
    {
        GameObject buildingGo = new GameObject($"Building_{data.DisplayName}");
        buildingGo.transform.SetParent(parent);
        buildingGo.transform.localPosition = new Vector3(0, 0.1f, 0); 
        buildingGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        buildingGo.transform.localRotation = Quaternion.Euler(0, 0, 0); 

        var sr = buildingGo.AddComponent<SpriteRenderer>();
        sr.sprite = data.buildingSprite; 
        sr.drawMode = SpriteDrawMode.Simple;

        var col = buildingGo.AddComponent<BoxCollider>();
        if (sr.sprite != null)
        {
            col.size = new Vector3(sr.bounds.size.x, sr.bounds.size.y, 2.0f);
            col.center = new Vector3(0, 0, -0.5f);
        }

        var bObj = buildingGo.AddComponent<BuildingObject>();
        bObj.Initialize(data, this);
    }
    
    // =========================================================================
    // 💾 [버그 원천 차단] 세이브 데이터 불러오기 전용 메소드
    // =========================================================================
    public void LoadBuilding(int slotIndex, BuildingData data, int level)
    {
        if (slotIndex < 0 || slotIndex >= constructionSlots.Length) return;
        Transform slotTransform = constructionSlots[slotIndex];

        if (slotTransform.TryGetComponent<SpriteRenderer>(out var mr)) mr.enabled = false;
        if (slotTransform.TryGetComponent<BoxCollider>(out var bc)) bc.enabled = false;
        if (slotTransform.TryGetComponent<ConstructionSlot>(out var cs)) cs.enabled = false;

        CreateBuildingVisual(slotTransform, data);
        var bObj = slotTransform.GetComponentInChildren<BuildingObject>();
    
        if (bObj != null)
        {
            // 💥 연출 없이 조용히 레벨만 셋업합니다.
            bObj.LoadLevelForce(level);
        }

        // 💥 [해결] 중복으로 ApplyBuildingEffect를 때리던 위험한 무한 중첩 코드를 완전 철거했습니다.
        // 이미 자원/인벤토리의 최대 수치는 데이터 세이브 매니저가 자체적으로 세이브/로드하고 있으므로
        // 여기서는 순수하게 겉모습 건물 체급 데이터만 맞춰주면 됩니다.
    }
    
    private void SaveSettlement()
    {
        var settSave = new SettlementSaveData();
        foreach (var slot in constructionSlots)
        {
            var bObj = slot.GetComponentInChildren<BuildingObject>();
            if (bObj != null)
                settSave.buildings.Add(new SettlementSaveData.BuildingSaveInfo { 
                    slotIndex = Array.IndexOf(constructionSlots, slot), 
                    type = bObj.Data.buildingType, 
                    level = bObj.CurrentLevel 
                });
        }
        SaveLoadSystem.Save(settSave);
    }

    public void SelectCharacter(BaseCharacter testHero)
    {
        _characterSelector.Select(testHero);
    }
}