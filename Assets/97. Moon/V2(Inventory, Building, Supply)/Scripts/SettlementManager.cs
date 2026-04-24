using UnityEngine;
using VContainer;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class SettlementManager : MonoBehaviour
{
    [SerializeField] private Transform[] constructionSlots; 
    
    private BuildingService _buildingService;
    private ResourceManager _resourceManager;
    private InventoryView _inventoryView;
    private ITotalInventory _totalInv;
    private IExpeditionInventory _expeditionInv;

    [Inject]
    public void Construct(
        BuildingService bs, 
        ResourceManager rm, 
        InventoryView iv, 
        ITotalInventory totalInv, 
        IExpeditionInventory expInv)
    {
        _buildingService = bs;
        _resourceManager = rm;
        _inventoryView = iv;
        _totalInv = totalInv;
        _expeditionInv = expInv;
    }

    private void Update()
    {
        if (Keyboard.current.tKey.wasPressedThisFrame) _resourceManager.Admin_AddAllResources(500);
    }

    // [중요] 기존 BuildingObject가 참조하던 메서드
    public void OnBuildingClicked(BuildingObject building)
    {
        Debug.Log($"<color=cyan>{building.Data.buildingName}</color> Lv.{building.CurrentLevel} 클릭됨.");
        var levelData = building.Data.GetLevelData(building.CurrentLevel);
        switch (building.Data.buildingType)
        {
            case BuildingType.Storage:
                _inventoryView.ToggleWindow(true); 
                break;
            case BuildingType.Guild:
                CollectGuildData(building);
                break;
            case BuildingType.Tavern: // 식당: HP 회복
                ProcessTavern(levelData.effectValue);
                break;
            case BuildingType.Inn:    // 여관: 스트레스(광기) 회복
                ProcessInn(levelData.effectValue);
                break;
            case BuildingType.Smithy: // 대장간: 장비 강화
                ProcessSmithy(building);
                break;
        }
    }

    public void BuildInSlot(int slotIndex, BuildingData data)
    {
        if (slotIndex < 0 || slotIndex >= constructionSlots.Length) return;
        Transform slotTransform = constructionSlots[slotIndex];
        if (slotTransform.childCount > 0) return;
    
        if (_buildingService.TryBuild(data))
        {
            // 시각적 처리
            if (slotTransform.TryGetComponent<MeshRenderer>(out var mr)) mr.enabled = false;
            if (slotTransform.TryGetComponent<BoxCollider>(out var bc)) bc.enabled = false;
    
            CreateBuildingVisual(slotTransform, data);
    
            // [추가] 최초 건설 시 1레벨 효과 적용
            ApplyBuildingEffect(data, 1); 
        }
    }
    
    // 업그레이드와 건설에서 공통으로 사용하는 효과 적용 메서드
    private void ApplyBuildingEffect(BuildingData data, int level)
    {
        var levelData = data.GetLevelData(level);
        Debug.Log($"<color=orange>[시스템]</color> {data.buildingName} Lv.{level} 효과 적용 시작");
    
        switch (data.buildingType)
        {
            case BuildingType.Storage:
                // 1. 전체 인벤토리 슬롯 확장
                _totalInv.ExpandStorage(levelData.slotExpansion);
                // 2. 자원 보유 상한치 확장
                _resourceManager.ExpandCapacities(levelData.frontierCapAdd, levelData.materialCapAdd, levelData.materialCapAdd);
                _inventoryView.RefreshUI(); // UI 갱신
                Debug.Log($"창고 효과: 슬롯 +{levelData.slotExpansion}, 상한치 확장됨");
                break;
    
            case BuildingType.Carriage:
                // 탐사 인벤토리 확장
                _expeditionInv.ExpandStorage(levelData.slotExpansion);
                _inventoryView.RefreshUI();
                Debug.Log($"마차 효과: 탐사 슬롯 +{levelData.slotExpansion}");
                break;
                
            case BuildingType.Guild:
                // 길드는 클릭 시 수급이므로 건설 직후 특별한 영구 효과가 없다면 비워둠
                break;
        }
    }
    
    // 건물을 클릭했을 때 호출되거나, 전용 UI에서 '휴식하기' 버튼을 눌렀을 때 호출
    public void ProcessRecovery(Stat characterStat, BuildingObject building)
    {
        var levelData = building.Data.GetLevelData(building.CurrentLevel);
        float power = levelData.effectValue;

        if (building.Data.buildingType == BuildingType.Tavern)
        {
            // 식당: HP 회복
            characterStat.RecoverHp((int)power);
            Debug.Log($"{building.Data.buildingName}: 체력을 {power}만큼 회복했습니다.");
        }
        else if (building.Data.buildingType == BuildingType.Inn)
        {
            // 여관: 스트레스(광기) 감소
            characterStat.RecoverInsanity((int)power);
            Debug.Log($"{building.Data.buildingName}: 광기를 {power}만큼 진정시켰습니다.");
        }
    }
    
    // 업그레이드 메서드도 이 공통 메서드를 사용하도록 리팩토링
    public void UpgradeBuilding(BuildingObject building)
    {
        int nextLevel = building.CurrentLevel + 1;
        if (nextLevel > building.Data.levels.Count) return;
    
        if (_buildingService.TryUpgrade(building.Data, nextLevel))
        {
            building.Upgrade();
            ApplyBuildingEffect(building.Data, nextLevel); // 공통 메서드 호출
        }
    }

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
            BuildingType.Tavern => new Color(0.5f, 1f, 0.5f), // 연두색
            BuildingType.Smithy => Color.red,
            BuildingType.Guild => new Color(0.5f, 0f, 1f),    // 보라색
            BuildingType.Storage => Color.blue,
            BuildingType.Carriage => Color.yellow,
            _ => Color.white
        };
    }

    private void CollectGuildData(BuildingObject guild)
    {
        // 길드 레벨에 따른 effectValue만큼 개척 데이터 즉시 수급
        int reward = (int)guild.Data.GetLevelData(guild.CurrentLevel).effectValue;
        _resourceManager.AddFrontierData(reward);
        Debug.Log($"길드에서 {reward}의 개척 데이터를 수급했습니다!");
    }
    
    private Stat _selectedCharacter; // 현재 선택된 캐릭터

    // 캐릭터 선택 (InteractionManager나 외부에서 호출)
    public void SelectCharacter(Stat characterStat)
    {
        _selectedCharacter = characterStat;
        Debug.Log($"<color=green>[선택됨]</color> {characterStat.ClassType} 클래스 캐릭터");
    }

    private Stat GetSelectedCharacterStat()
    {
        if (_selectedCharacter == null)
        {
            Debug.LogWarning("선택된 캐릭터가 없습니다! 캐릭터를 먼저 클릭하세요.");
        }
        return _selectedCharacter;
    }

    // --- 건물 실무 로직 완결 ---

    private void ProcessTavern(float effectValue)
    {
        Stat target = GetSelectedCharacterStat();
        if (target == null) return;

        // 식당: HP 회복 (effectValue만큼)
        // StatCalculate를 호출하여 최대 체력을 갱신한 뒤 회복
        target.StatCalculate(); 
        // target.Heal((int)effectValue); // 별도의 Heal 메서드가 있다면 사용
        Debug.Log($"식당 이용: {target.ClassType} HP 회복 완료");
    }

    private void ProcessInn(float effectValue)
    {
        Stat target = GetSelectedCharacterStat();
        if (target == null) return;

        // 여관: 스트레스(광기) 감소
        target.insanity = Mathf.Max(0, target.insanity - (int)effectValue);
        Debug.Log($"여관 이용: {target.ClassType} 스트레스 {effectValue} 감소");
    }

    private void ProcessSmithy(BuildingObject smithy)
    {
        Stat target = GetSelectedCharacterStat();
        if (target == null) return;

        // 대장간: 현재 대장간 레벨까지만 장비 강화 허용
        // 예: 무기(baseWeapon) 강화
        if (target.baseWeapon != null && target.baseWeapon.upgradeLevel < smithy.CurrentLevel)
        {
            target.baseWeapon.Upgrade();
            target.StatCalculate(); // 강화 수치 반영을 위한 재계산
            Debug.Log($"{target.ClassType} 무기 강화 성공! (Lv.{target.baseWeapon.upgradeLevel})");
        }
        else
        {
            Debug.LogWarning("강화 불가: 대장간 레벨 부족 또는 최대 강화 상태");
        }
    }
}