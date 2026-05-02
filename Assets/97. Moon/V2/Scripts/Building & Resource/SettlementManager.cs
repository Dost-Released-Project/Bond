using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;

public class SettlementManager : MonoBehaviour, ISettlementManager
{
    [SerializeField] private Transform[] constructionSlots; 
    
    private BuildingService _buildingService;
    private ResourceManager _resourceManager;
    private InventoryView _inventoryView;
    private SupplyView _supplyView;
    private ITotalInventory _totalInv;
    private IExpeditionInventory _expeditionInv;
    
    private BaseCharacter _selectedCharacter;

    [Inject]
    public void Construct(BuildingService bs, ResourceManager rm, InventoryView iv, SupplyView supply, ITotalInventory total, IExpeditionInventory exp)
    {
        _buildingService = bs; _resourceManager = rm; _inventoryView = iv;
        _supplyView = supply; _totalInv = total; _expeditionInv = exp;
    }
    
    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame) _resourceManager.Admin_AddAllResources(1000);
    }

    public void OnBuildingClicked(BuildingObject building)
    {
        var levelData = building.Data.GetLevelData(building.CurrentLevel);
        
        // 분기 처리: UI 오픈 vs 실제 기능 실행
        switch (building.Data.buildingType)
        {
            case BuildingType.Storage: _inventoryView.ToggleWindow(true); break;
            case BuildingType.Supply: _supplyView.Open(); break;
            case BuildingType.Tavern: _buildingService.ExecuteTavern(_selectedCharacter, levelData); break;
            case BuildingType.Inn: _buildingService.ExecuteInn(_selectedCharacter, levelData); break;
            case BuildingType.Smithy: ProcessSmithy(building); break;
            case BuildingType.Guild: CollectGuildData(building); break;
        }
    }

    public void BuildInSlot(int slotIndex, BuildingData data)
    {
        Debug.Log($"<color=white>[시스템]</color> 슬롯 {slotIndex}에 {data.buildingName} 건설 시도 중...");

        if (slotIndex < 0 || slotIndex >= constructionSlots.Length)
        {
            Debug.LogError($"[건설 실패] 유효하지 않은 슬롯 인덱스: {slotIndex}");
            return;
        }

        Transform slotTransform = constructionSlots[slotIndex];

        // 이미 건물이 있는지 체크
        if (slotTransform.childCount > 0)
        {
            Debug.LogWarning($"[건설 실패] 슬롯 {slotIndex}에 이미 건물이 존재합니다.");
            return;
        }

        // 서비스에서 자원 소모 및 성공 여부 확인
        if (_buildingService.TryBuild(data))
        {
            Debug.Log($"<color=green>[건설 성공]</color> {data.buildingName} 건설을 시작합니다.");

            // [복구] 기존 슬롯 시각적 요소 및 기능 제거
            // 슬롯 컴포넌트나 메쉬를 꺼서 더 이상 건설 창이 뜨지 않게 합니다.
            if (slotTransform.TryGetComponent<MeshRenderer>(out var mr)) mr.enabled = false;
            if (slotTransform.TryGetComponent<BoxCollider>(out var bc)) bc.enabled = false;
        
            // 만약 슬롯 자체가 UI를 여는 스크립트를 가지고 있다면 비활성화
            if (slotTransform.TryGetComponent<ConstructionSlot>(out var slotScript)) slotScript.enabled = false;

            // 실제 건물 비주얼 생성
            CreateBuildingVisual(slotTransform, data);

            // 최초 건설 시 1레벨 효과 적용
            ApplyBuildingEffect(data, 1); 
        
            Debug.Log($"<color=cyan>[시스템]</color> {data.buildingName} 배치가 완료되었습니다.");
        }
        else
        {
            Debug.LogWarning($"[자원 부족] {data.buildingName}을 건설하기 위한 자원이 모자랍니다.");
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

    private void ProcessSmithy(BuildingObject smithy)
    {
        if (_selectedCharacter == null) return;
        Equipment target = AdminTestTool.isTargetingWeapon ? _selectedCharacter.Stat.baseWeapon : _selectedCharacter.Stat.baseArmor;
        _buildingService.UpgradeEquipment(_selectedCharacter.Stat, target, smithy.CurrentLevel);
    }
    
    private void CollectGuildData(BuildingObject guild)
    {
        // 길드 레벨에 따른 effectValue만큼 개척 데이터 즉시 수급
        int reward = guild.Data.GetLevelData(guild.CurrentLevel).effectValue;
        _resourceManager.AddFrontierData(reward);
        _resourceManager.AddMaterials((int)(reward*0.05f), (int)(reward*0.05f));
        Debug.Log($"길드에서 {reward}의 개척 데이터를 수급했습니다!");
    }

    public void SelectCharacter(BaseCharacter character) => _selectedCharacter = character;
    
    // CreateBuildingVisual 및 기타 헬퍼 메서드 (기능 유지)
    private void CreateBuildingVisual(Transform parent, BuildingData data)
    {
        GameObject buildingGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
        buildingGo.name = $"Building_{data.buildingName}";
        buildingGo.transform.SetParent(parent);
        buildingGo.transform.localPosition = new Vector3(0, 0.5f, 0); 
        
        var bObj = buildingGo.AddComponent<BuildingObject>();
        bObj.Initialize(data, this);

        var renderer = buildingGo.GetComponent<Renderer>();
        renderer.material.color = GetColorByBuildingType(data.buildingType);
    }

    private Color GetColorByBuildingType(BuildingType type)
    {
        return type switch
        {
            BuildingType.Inn => Color.green,
            BuildingType.Tavern => Color.orange,
            BuildingType.Smithy => Color.red,
            BuildingType.Guild => Color.purple, 
            BuildingType.Storage => Color.blue,
            BuildingType.Carriage => Color.yellow,
            BuildingType.Supply => Color.black,
            _ => Color.white
        };
    }
}