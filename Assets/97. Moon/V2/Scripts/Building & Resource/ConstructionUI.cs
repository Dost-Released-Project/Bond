using System;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using System.IO;
using Bond.Persistence;
using UnityEngine.AddressableAssets;

public class ConstructionUI : MonoBehaviour
{
    private BuildingDataBaseSO _buildingDB;

    private VisualElement _root;
    [Inject] private SettlementManager _settlementManager;
    [Inject] private CharacterSelector _characterSelector; // 👥 여관/주점 영웅 상태 추적용 주입
    
    private int _targetSlotIndex;
    private BuildingData _targetBuildingData; 
    private BuildingObject _targetBuildingObject; // 📈 업그레이드/사용 대상 캐싱

    // 💥 [통합 UI Toolkit 레이블 & 버튼 구조 정돈]
    private Label _lblBuildingName;
    private Label _lblBuildingDescription;
    private Label _lblBuildingCost;
    private Button _btnConfirm;
    private Button _btnCancel;

    // 💥 내부 팝업 상태 분기 제어용 열거형
    private enum PopupMode { Construction, Upgrade, Interaction }
    private PopupMode _currentMode;
    
    private async void Awake()
    {
        var handle = Addressables.LoadAssetAsync<BuildingDataBaseSO>("BuildingDataBase");
        _buildingDB = await handle.Task;
        LoadSettlement();
    }

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.style.display = DisplayStyle.None;

        _lblBuildingName = _root.Q<Label>("construction-name");
        _lblBuildingDescription = _root.Q<Label>("construction-desc");
        _lblBuildingCost = _root.Q<Label>("construction-cost");
        
        _btnConfirm = _root.Q<Button>("btn-confirm"); 
        _btnCancel = _root.Q<Button>("btn-cancel");   

        if (_btnConfirm != null) _btnConfirm.clicked += OnConfirmClicked;
        if (_btnCancel != null) _btnCancel.clicked += () => Show(false);
        
        var btnClose = _root.Q<Button>("btn-close");
        if (btnClose != null) btnClose.clicked += () => Show(false);
    }

    // =========================================================================
    // 🏗️ 1. [건설 팝업 모드]
    // =========================================================================
    public void OpenConstruction(int slotIndex, BuildingType type)
    {
        if (_buildingDB == null) return;

        _currentMode = PopupMode.Construction;
        _targetSlotIndex = slotIndex;
        _targetBuildingData = _buildingDB.FindSO<BuildingData>(d => d.buildingType == type);

        if (_targetBuildingData == null) return;

        _lblBuildingName.text = $"{_targetBuildingData.DisplayName} 건설";
        _lblBuildingDescription.text = string.IsNullOrEmpty(_targetBuildingData.Description) 
            ? "영지의 특수 기능을 담당하는 건물입니다." 
            : _targetBuildingData.Description;

        var firstLevelData = _targetBuildingData.GetLevelData(1);
        if (firstLevelData.level != 0)
        {
            _lblBuildingCost.text = $"[건설 비용]\n개척 데이터: {firstLevelData.frontierCost} | 목재: {firstLevelData.woodCost} | 광물: {firstLevelData.oreCost}";
        }

        Show(true);
    }
    
    // =========================================================================
    // 📈 2. [업그레이드 팝업 모드] (우클릭 시 발동 - 툴팁 누적 공식 이식 완료)
    // =========================================================================
    public void OpenUpgrade(BuildingObject building)
    {
        if (building == null || building.Data == null) return;

        _currentMode = PopupMode.Upgrade;
        _targetBuildingObject = building;
        _targetBuildingData = building.Data;

        int curLevel = building.CurrentLevel;
        int nextLevel = curLevel + 1;
        
        // 만렙 예외 차단
        if (nextLevel > _targetBuildingData.levels.Count)
        {
            Debug.LogWarning("이미 최고 레벨인 건물입니다.");
            return;
        }

        _lblBuildingName.text = $"{_targetBuildingData.DisplayName} 업그레이드 (Lv.{curLevel} ➔ Lv.{nextLevel})";
        
        // 1. 1레벨부터 현재 레벨까지의 고유 효과 누적 합산 연산
        int totalMaterialCap = 0;
        int totalFrontierCap = 0;
        int totalSlotExpansion = 0;
        int totalEffectValue = 0;

        for (int i = 1; i <= curLevel; i++)
        {
            var levelData = _targetBuildingData.GetLevelData(i);
            if (levelData.level != 0)
            {
                totalMaterialCap += levelData.materialCapAdd;
                totalFrontierCap += levelData.frontierCapAdd;
                totalSlotExpansion += levelData.slotExpansion;
                totalEffectValue += levelData.effectValue;
            }
        }

        // 2. 다음 레벨 데이터 기반 최종 수치 누적 계산
        var nextLvData = _targetBuildingData.GetLevelData(nextLevel);
        
        int nextMaterialCap = totalMaterialCap + nextLvData.materialCapAdd;
        int nextFrontierCap = totalFrontierCap + nextLvData.frontierCapAdd;
        int nextSlotExpansion = totalSlotExpansion + nextLvData.slotExpansion;
        int nextEffectValue = totalEffectValue + nextLvData.effectValue;
        int curMaxUses = _targetBuildingData.GetLevelData(curLevel).maxUses;

        // 3. 툴팁 스펙 가이드라인 1대1 맵핑 텍스트 조립
        string upText = "[업그레이드 완료 시 최종 변경값]\n";
        
        if (nextLvData.materialCapAdd > 0) upText += $"- 자원 보관 한도: {totalMaterialCap} ➔ {nextMaterialCap} 증가\n";
        if (nextLvData.frontierCapAdd > 0) upText += $"- 개척 가능 한도: {totalFrontierCap} ➔ {nextFrontierCap} 증가\n";
        if (nextLvData.slotExpansion > 0)  upText += $"- 인벤토리 슬롯: {totalSlotExpansion}칸 ➔ {nextSlotExpansion}칸 증가\n";
        if (nextLvData.effectValue > 0)   upText += $"- 효과 고유 수치: {totalEffectValue} ➔ {nextEffectValue} 증가\n";
        
        if (_targetBuildingData.buildingType == BuildingType.Smithy || _targetBuildingData.name.Contains("Smithy"))
        {
            upText += $"- 장비 최고 강화 한도: {curLevel}단계 ➔ {nextLevel}단계 제한 확장\n";
        }
        
        // 이용 제한 횟수 (합산이 아닌 테이블 단발성 상태 수치 추적 규칙 적용)
        if (curMaxUses > 0 && nextLvData.maxUses != curMaxUses)
        {
            upText += $"- 이용 제한 횟수: {curMaxUses}회 ➔ {nextLvData.maxUses}회로 제한 변경\n";
        }

        _lblBuildingDescription.text = upText;

        // 4. 다음 레벨 진입 비용 파싱 기입
        _lblBuildingCost.text = $"[업그레이드 비용]\n개척 데이터: {nextLvData.frontierCost} | 목재: {nextLvData.woodCost} | 광물: {nextLvData.oreCost}";

        Show(true);
    }

    // =========================================================================
    // 🍹 3. [상호작용 효과 사용 팝업 모드] (주점, 여관, 길드 전용 검증 레이어)
    // =========================================================================
    public void OpenInteraction(BuildingObject building)
    {
        if (building == null || building.Data == null) return;

        _currentMode = PopupMode.Interaction;
        _targetBuildingObject = building;
        _targetBuildingData = building.Data;

        var lvData = _targetBuildingData.GetLevelData(building.CurrentLevel);
        _lblBuildingName.text = $"{_targetBuildingData.DisplayName} 기능 이용";

        // 분기 A: 길드 데이터 즉시 즉각 정산 안내
        if (_targetBuildingData.buildingType == BuildingType.Guild)
        {
            _lblBuildingDescription.text = "길드에 축적된 개척 보고서를 분석하여 영지 데이터를 수집합니다.";
            _lblBuildingCost.text = $"[수령 보상] 개척 데이터: +{lvData.effectValue} 수급 완료 가능";
        }
        // 분기 B: 주점(Tavern), 여관(Inn) 영웅 상태 기반 조건부 회복 안내
        else
        {
            BaseCharacter selectedHero = _characterSelector?.Selected;
            if (selectedHero == null)
            {
                _lblBuildingDescription.text = "<color=red>선택된 영웅이 없습니다. 이용할 영웅을 먼저 선택해 주세요.</color>";
                _lblBuildingCost.text = "이용 불가";
            }
            else
            {
                int preValue;
                // (※ 프로젝트의 실제 정신력 변수명 규격에 맞게 매핑)
                if (_targetBuildingData.buildingType == BuildingType.Inn)
                {
                    // 기획안 반영: 이름, 현재 HP, 정신력, 건물 회복량 및 회복 이후 변경값 가이드 연산
                    preValue = Mathf.Min(selectedHero.Stat.current_Hp + lvData.effectValue, selectedHero.Stat.max_Hp);
                    
                    _lblBuildingDescription.text = $"이용 영웅: <b>{selectedHero.Name}</b>\n" +
                                                   $"현재 상태 ➔ 체력: {selectedHero.Stat.current_Hp}/{selectedHero.Stat.max_Hp}\n\n" +
                                                   $"[회복 피드백]\n효과 적용 시 체력이 {lvData.effectValue}만큼 회복됩니다.\n" +
                                                   $"(예상 변경치 ➔ 체력: {selectedHero.Stat.current_Hp} ➔ {preValue})\n";
                }
                else
                {
                    preValue = Mathf.Max(selectedHero.Insanity - lvData.effectValue, 0);
                    _lblBuildingDescription.text = $"이용 영웅: <b>{selectedHero.Name}</b>\n" +
                                                   $"현재 상태 ➔ 광기: {selectedHero.Insanity}\n\n" +
                                                   $"[회복 피드백]\n효과 적용 시 광기가 {lvData.effectValue}만큼 감소합니다.\n" +
                                                   $"(예상 변경치 ➔ 광기: {selectedHero.Insanity} ➔ {preValue})";
                }

                int remainUses = lvData.maxUses - (building.Counter != null ? building.Counter.CurrentTurnUses : 0);
                _lblBuildingCost.text = $"이번 턴 남은 이용 가능 횟수: {remainUses} / {lvData.maxUses}회";
            }
        }

        Show(true);
    }

    // =========================================================================
    // 🎯 [최종 확인 버튼 공용 클릭 액션 분기 허브]
    // =========================================================================
    private void OnConfirmClicked()
    {
        switch (_currentMode)
        {
            case PopupMode.Construction:
                _settlementManager.BuildInSlot(_targetSlotIndex, _targetBuildingData);
                break;

            case PopupMode.Upgrade:
                _settlementManager.UpgradeBuilding(_targetBuildingObject);
                break;

            case PopupMode.Interaction:
                // 💥 주점, 여관, 길드 실제 비즈니스 기능 로직을 실행하도록 세틀먼트매니저에게 토스
                if (_targetBuildingObject != null)
                {
                    _settlementManager.ExecuteInteractionDirect(_targetBuildingObject);
                }
                break;
        }
        Show(false);
    }

    private void Show(bool isShow) => _root.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
    public bool IsAnyPopupOpen => _root != null && _root.style.display == DisplayStyle.Flex;

    private void LoadSettlement()
    {
        var save = new SettlementSaveData();
        string saveKey = save.Key;
        string path = Path.Combine(Application.dataPath, "Data", "Save", $"{saveKey}.json");
        if (File.Exists(path))
        {
            try
            {
                SaveLoadSystem.Load(save);
                foreach (var b in save.buildings)
                {
                    var data = _buildingDB.FindSO<BuildingData>(d => d.buildingType == b.type);
                    if (data != null) _settlementManager.LoadBuilding(b.slotIndex, data, b.level);
                }
                Debug.Log("Settlement: 데이터 로드 성공");
            }
            catch (Exception e)
            {
                Debug.LogError($"Settlement: 로드 중 오류 발생 - {e.Message}");
            }
        }
    }
}