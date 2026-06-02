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
    private ConstructionUI _constructionUI;
    
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
    
    private void Start()
    {
        // 💥 [소스 매핑] OnSelectionChanged 이벤트를 구독합니다.
        // 이벤트가 던져주는 BaseCharacter 데이터는 무시하고, 요청하신 '매개변수 없는 감지 메서드'를 실행하도록 엮습니다.
        if (_characterSelector != null)
        {
            _characterSelector.OnSelectionChanged += OnCharacterSelectionChanged;
        }

        _constructionUI = FindAnyObjectByType<ConstructionUI>();
    }

    private void OnDestroy()
    {
        // 메모리 누수 방지 구독 해제
        if (_characterSelector != null)
        {
            _characterSelector.OnSelectionChanged -= OnCharacterSelectionChanged;
        }
    }

    // 💥 [규칙 완벽 준수] 매개변수를 갖지 않는 순수 변경 감지 및 대장간 UI 리프레시 메서드
    private void OnCharacterSelectionChanged(BaseCharacter _)
    {
        // 1. 대장간 UI가 화면에 켜져 있는 상태(IsOpen)인지 레이어 검사
        if (_smithyUI != null && _smithyUI.IsOpen)
        {
            // 2. 현재 셀렉터 시스템에 안착한 최신 선택 캐릭터 호출
            BaseCharacter activeHero = _characterSelector.Selected;
            
            if (activeHero != null)
            {
                Debug.Log($"<color=lime>[대장간 실시간 감지]</color> 선택 캐릭터가 <b>{activeHero.Name}</b>(으)로 변경되어 대장간 UI를 리프레시합니다.");
                
                // 3. 대장간 UI 세부 정보 전환 갱신
                _smithyUI.ChangeCharacter(activeHero);
            }
        }
        
        // 캐릭터가 바뀔 때 대장간뿐만 아니라, 주점/여관 기능 창도 실시간으로 가이드 수치를 갱신합니다!
        if (_constructionUI != null)
        {
            _constructionUI.RefreshCurrentInteraction();
        }
    }
    
    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame) _resourceManager.Admin_AddAllResources(1000);
        if (Keyboard.current.pKey.wasPressedThisFrame) OnExpeditionReturned(); // 치트 테스트
    }
    
    public void OnBuildingClicked(BuildingObject building)
    {
        if (building.Counter != null && building.Counter.IsUseLimitReached())
        {
            Debug.LogWarning($"[이용 불가] {building.Data.DisplayName}의 이번 턴 이용 한도를 초과했습니다!");
            return;
        }

        // 💥 [핵심 기획 교정 분기]
        // 주점, 여관, 길드는 클릭 즉시 실행하지 않고, 리더님 기획대로 상세 확인 팝업창을 먼저 띄웁니다.
        switch (building.Data.buildingType)
        {
            case BuildingType.Tavern:
            case BuildingType.Inn:
            case BuildingType.Guild:
                _smithyUI.Close(); // 대장간 열려있으면 안전하게 닫기
                _supplyView.Close();
                _constructionUI.OpenInteraction(building); // 팝업창 토스!
                break;

            // 창고나 보급소, 대장간 등 전용 UI가 별개로 존재하는 기물들은 기존 규칙 유지
            case BuildingType.Storage: 
                _inventoryView.ToggleWindow(true); 
                break;
            case BuildingType.Supply: 
                _smithyUI.Close();
                _constructionUI.Show(false);
                _supplyView.Open();
                break;
            case BuildingType.Smithy:
                _constructionUI.Show(false);
                _supplyView.Close();
                _smithyUI.Open(_characterSelector.Selected, building.CurrentLevel);
                break;
        }
    }

    // =========================================================================
    // 🍹 [신규 추가] 팝업창 확인 버튼(Confirm)을 눌렀을 때 비로소 실행되는 진짜 기능 수용소
    // =========================================================================
    public void ExecuteInteractionDirect(BuildingObject building)
    {
        var levelData = building.Data.GetLevelData(building.CurrentLevel);
        bool isUsed = false;

        switch (building.Data.buildingType)
        {
            case BuildingType.Tavern:
                if (_characterSelector.Selected != null)
                {
                    _buildingService.ExecuteTavern(_characterSelector.Selected, levelData);
                    if (building.Counter != null) { building.Counter.UseBuilding(); isUsed = true; }
                }
                break;

            case BuildingType.Inn:
                if (_characterSelector.Selected != null)
                {
                    _buildingService.ExecuteInn(_characterSelector.Selected, levelData);
                    if (building.Counter != null) { building.Counter.UseBuilding(); isUsed = true; }
                }
                break;

            case BuildingType.Guild:
                CollectGuildData(building);
                if (building.Counter != null) { building.Counter.UseBuilding(); isUsed = true; }
                break;
        }

        // 팝업이 닫힌 뒤 시각 부품에게 강제 툴팁 리프레시 신호 송출
        if (isUsed && building.Visuals != null)
        {
            building.Visuals.ForceRefreshTooltip();
        }
    }

    public void OnExpeditionReturned()
    {
        Debug.Log("<color=green>[원정대 복귀]</color> 모든 영지 건물의 이용 횟수가 리셋됩니다.");
        foreach (var slot in constructionSlots)
        {
            var bObj = slot.GetComponentInChildren<BuildingObject>();
            // 하위 Counter 부품만 콕 집어서 턴 리셋 진행
            if (bObj != null && bObj.Counter != null)
            {
                bObj.Counter.ResetTurnUses();
            }
        }
    }

    public void BuildInSlot(int slotIndex, BuildingData data)
    {
        if (slotIndex < 0 || slotIndex >= constructionSlots.Length) return;
        Transform slotTransform = constructionSlots[slotIndex];
        if (slotTransform.childCount > 0) return;

        if (_buildingService.TryBuild(data))
        {
            if (slotTransform.TryGetComponent<SpriteRenderer>(out var mr)) mr.enabled = false;
            if (slotTransform.TryGetComponent<BoxCollider>(out var bc)) bc.enabled = false;
            if (slotTransform.TryGetComponent<ConstructionSlot>(out var slotScript)) slotScript.enabled = false;

            // 💥 팩토리를 통해 안전하게 부품화된 건물 생성
            BuildingObject bObj = BuildingFactory.Create(slotTransform, data, this);
            
            // 생성 완료 시점에 시각 부품에게 두근 연출 수동 명령
            if (bObj != null && bObj.Visuals != null)
            {
                bObj.Visuals.TriggerConstructionPopping(1);
            }

            ApplyBuildingEffect(data, 1);
            SaveSettlement();
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
            
            // 💥 업그레이드가 성공한 바로 그 즉시 딱 1번 툴팁을 새 레벨 정보로 리프레시!
            if (building.Visuals != null)
            {
                building.Visuals.ForceRefreshTooltip();
            }
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
        Debug.Log($"길드에서 {reward}의 개척 데이터와 {reward*0.05f}의 재료를 수급했습니다!");
    }
    
    public void LoadBuilding(int slotIndex, BuildingData data, int level)
    {
        if (slotIndex < 0 || slotIndex >= constructionSlots.Length) return;
        Transform slotTransform = constructionSlots[slotIndex];

        if (slotTransform.TryGetComponent<SpriteRenderer>(out var mr)) mr.enabled = false;
        if (slotTransform.TryGetComponent<BoxCollider>(out var bc)) bc.enabled = false;
        if (slotTransform.TryGetComponent<ConstructionSlot>(out var cs)) cs.enabled = false;

        // 💥 팩토리로 조용히 조립 생산
        BuildingObject bObj = BuildingFactory.Create(slotTransform, data, this);
    
        if (bObj != null)
        {
            bObj.LoadLevelForce(level);
        }
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
}