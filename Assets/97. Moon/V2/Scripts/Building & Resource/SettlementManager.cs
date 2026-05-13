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
    
    [Inject] private SmithyUIController _smithyUI; // UI 컨트롤러 주입
    private BaseCharacter _selectedCharacter;

    [Inject]
    public void Construct(BuildingService bs, ResourceManager rm, InventoryView iv, SupplyView supply, ITotalInventory total, IExpeditionInventory exp)
    {
        _buildingService = bs; _resourceManager = rm; _inventoryView = iv;
        _supplyView = supply; _totalInv = total; _expeditionInv = exp;
    }
    
    private void Awake()
    {
        // "ConstructionSlot" 태그가 붙은 모든 오브젝트를 찾아 이름순으로 정렬하여 배열에 할당
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
        
        // 분기 처리: UI 오픈 vs 실제 기능 실행
        switch (building.Data.buildingType)
        {
            case BuildingType.Storage: _inventoryView.ToggleWindow(true); break;
            case BuildingType.Supply: _supplyView.Open(); break;
            case BuildingType.Tavern: _buildingService.ExecuteTavern(_selectedCharacter, levelData); break;
            case BuildingType.Inn: _buildingService.ExecuteInn(_selectedCharacter, levelData); break;
            case BuildingType.Smithy: // 이제 직접 강화하지 않고 UI를 엽니다.
                // 선택된 캐릭터와 현재 대장간 건물 레벨을 전달합니다.
                if (_selectedCharacter != null)
                    _smithyUI.Open(_selectedCharacter, building.CurrentLevel);; break;
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

        // 이미 건물이 있는지 체크
        if (slotTransform.childCount > 0)
        {
            Debug.LogWarning($"[건설 실패] 슬롯 {slotIndex}에 이미 건물이 존재합니다.");
            return;
        }

        // 서비스에서 자원 소모 및 성공 여부 확인
        if (_buildingService.TryBuild(data))
        {
            Debug.Log($"<color=green>[건설 성공]</color> {data.DisplayName} 건설을 시작합니다.");

            // [복구] 기존 슬롯 시각적 요소 및 기능 제거
            // 슬롯 컴포넌트나 메쉬를 꺼서 더 이상 건설 창이 뜨지 않게 합니다.
            if (slotTransform.TryGetComponent<MeshRenderer>(out var mr)) mr.enabled = false;
            if (slotTransform.TryGetComponent<BoxCollider>(out var bc)) bc.enabled = false;
            if (slotTransform.TryGetComponent<ConstructionSlot>(out var slotScript)) slotScript.enabled = false;

            // 실제 건물 비주얼 생성
            CreateBuildingVisual(slotTransform, data);

            // 최초 건설 시 1레벨 효과 적용
            ApplyBuildingEffect(data, 1);
            
            // 데이터 세이브
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
            
            //데이터 세이브
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
        // 길드 레벨에 따른 effectValue만큼 개척 데이터 즉시 수급
        int reward = guild.Data.GetLevelData(guild.CurrentLevel).effectValue;
        _resourceManager.AddFrontierData(reward);
        _resourceManager.AddMaterials((int)(reward*0.05f), (int)(reward*0.05f));
        Debug.Log($"길드에서 {reward}의 개척 데이터를 수급했습니다!");
    }

    public void SelectCharacter(BaseCharacter character) => _selectedCharacter = character;
    
    // CreateBuildingVisual 및 기타 헬퍼 메서드 (기능 유지)
    // private void CreateBuildingVisual(Transform parent, BuildingData data)
    // {
    //     GameObject buildingGo = GameObject.CreatePrimitive(PrimitiveType.Cube);
    //     buildingGo.name = $"Building_{data.DisplayName}";
    //     buildingGo.transform.SetParent(parent);
    //     buildingGo.transform.localPosition = new Vector3(0, 0.5f, 0); 
    //     
    //     var bObj = buildingGo.AddComponent<BuildingObject>();
    //     bObj.Initialize(data, this);
    //
    //     var renderer = buildingGo.GetComponent<Renderer>();
    //     renderer.material.color = GetColorByBuildingType(data.buildingType);
    // }
    
    // 스프라이트로 건물 생성
    private void CreateBuildingVisual(Transform parent, BuildingData data)
    {
        // 1. 빈 오브젝트 생성
        GameObject buildingGo = new GameObject($"Building_{data.DisplayName}");
        buildingGo.transform.SetParent(parent);
        buildingGo.transform.localPosition = Vector3.zero; // 바닥에 붙임

        // 2. 스프라이트 렌더러 추가 및 설정
        var sr = buildingGo.AddComponent<SpriteRenderer>();
        sr.sprite = data.buildingSprite; // BuildingData에 추가하신 스프라이트
        sr.drawMode = SpriteDrawMode.Simple;
    
        // 건물이 땅 뚫고 들어가지 않게 위치 조정 (스프라이트 크기에 따라 pivot 조정 필요)
        buildingGo.transform.localPosition = new Vector3(0, 0.1f, 0);
        buildingGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        // 쿼터뷰/탑다운일 경우 카메라를 바라보게 회전 (필요시)
        buildingGo.transform.rotation = Quaternion.Euler(0, 0, 0); 

        // 3. 클릭 감지를 위한 콜라이더 추가 (레이캐스트용)
        var col = buildingGo.AddComponent<BoxCollider>();
        // 스프라이트 크기에 맞춰 콜라이더 사이즈 자동 조정
        col.size = new Vector3(sr.bounds.size.x, sr.bounds.size.y, 0.1f);

        // 4. 기능 컴포넌트 부착
        var bObj = buildingGo.AddComponent<BuildingObject>();
        bObj.Initialize(data, this);
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
    
    // 세이브 데이터 불러오기 전용 메소드
    public void LoadBuilding(int slotIndex, BuildingData data, int level)
    {
        if (slotIndex < 0 || slotIndex >= constructionSlots.Length) return;
        Transform slotTransform = constructionSlots[slotIndex];

        // 1. 슬롯 비활성화 (겹침 방지)
        if (slotTransform.TryGetComponent<MeshRenderer>(out var mr)) mr.enabled = false;
        if (slotTransform.TryGetComponent<BoxCollider>(out var bc)) bc.enabled = false;
        if (slotTransform.TryGetComponent<ConstructionSlot>(out var cs)) cs.enabled = false;

        // 2. 비주얼 생성 및 초기화
        CreateBuildingVisual(slotTransform, data);
        var bObj = slotTransform.GetComponentInChildren<BuildingObject>();
    
        // 3. 레벨 강제 설정 (Upgrade 메서드 반복 호출 혹은 직접 대입)
        for (int i = 1; i < level; i++) bObj.Upgrade(); 
    }
    
    // 데이터 세이브
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
}