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

    private void Start()
    {
        _buildingDB = DBSORegistry.GetDb<BuildingDataBaseSO>();
        if (_buildingDB == null)
        {
            Debug.LogError("[ConstructionUI] BuildingDataBaseSO가 레지스트리에 로드되어 있지 않습니다. 부트스트랩을 확인하세요.");
        }
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
    // 📈 2. [업그레이드 팝업 모드] (유니티 6 전용 Rich Text 행간 보정판)
    // =========================================================================
    public void OpenUpgrade(BuildingObject building)
    {
        if (building == null || building.Data == null) return;

        _currentMode = PopupMode.Upgrade;
        _targetBuildingObject = building;
        _targetBuildingData = building.Data;

        int curLevel = building.CurrentLevel;
        int nextLevel = curLevel + 1;

        if (nextLevel > _targetBuildingData.levels.Count)
        {
            Debug.LogWarning("이미 최고 레벨인 건물입니다.");
            return;
        }

        _lblBuildingName.text = $"{_targetBuildingData.DisplayName} 업그레이드 (Lv.{curLevel} ➔ Lv.{nextLevel})";

        // 1. [누적형 데이터] 합산 연산
        int totalMaterialCap = 0;
        int totalFrontierCap = 0;
        int totalSlotExpansion = 0;

        for (int i = 1; i <= curLevel; i++)
        {
            var levelData = _targetBuildingData.GetLevelData(i);
            if (levelData.level != 0)
            {
                totalMaterialCap += levelData.materialCapAdd;
                totalFrontierCap += levelData.frontierCapAdd;
                totalSlotExpansion += levelData.slotExpansion;
            }
        }

        // 2. [단발성 데이터] 파싱
        var curLvData = _targetBuildingData.GetLevelData(curLevel);
        var nextLvData = _targetBuildingData.GetLevelData(nextLevel);

        int nextMaterialCap = totalMaterialCap + nextLvData.materialCapAdd;
        int nextFrontierCap = totalFrontierCap + nextLvData.frontierCapAdd;
        int nextSlotExpansion = totalSlotExpansion + nextLvData.slotExpansion;

        int curEffectValue = curLvData.effectValue;
        int nextEffectValue = nextLvData.effectValue;
        int curMaxUses = curLvData.maxUses;

        // =========================================================================
        // 🎯 [유니티 6 핵심 해결책] Rich Text 태그로 강제 줄간격 확보 규칙 주입
        // 문자열 전체를 <line-height=110%> 태그로 감싸주면 40px 글자 크기 버그를 무시하고 
        // 줄바꿈 시 글자가 서로 겹치지 않게 물리적 공간을 완벽히 확보합니다.
        // =========================================================================
        string upText = "<line-height=120%>[업그레이드 완료 시 최종 변경값]\n";

        if (nextLvData.materialCapAdd > 0) upText += $"- 자원 보관 한도: {totalMaterialCap} ➔ {nextMaterialCap} 증가\n";
        if (nextLvData.frontierCapAdd > 0) upText += $"- 개척 가능 한도: {totalFrontierCap} ➔ {nextFrontierCap} 증가\n";
        if (nextLvData.slotExpansion > 0) upText += $"- 인벤토리 슬롯: {totalSlotExpansion}칸 ➔ {nextSlotExpansion}칸 증가\n";
        if (nextLvData.effectValue > 0) upText += $"- 효과 고유 수치: {curEffectValue} ➔ {nextEffectValue} 변경\n";

        if (_targetBuildingData.buildingType == BuildingType.Smithy || _targetBuildingData.name.Contains("Smithy"))
        {
            upText += $"- 장비 최고 강화 한도: {curLevel}단계 ➔ {nextLevel}단계 제한 확장\n";
        }

        if (curMaxUses > 0 && nextLvData.maxUses != curMaxUses)
        {
            upText += $"- 이용 제한 횟수: {curMaxUses}회 ➔ {nextLvData.maxUses}회로 제한 변경\n";
        }

        // 태그 닫기
        upText += "</line-height>";

        // 3. 콤팩트한 단발 대입 (레이블 분할 코드 다 철거하여 단순화)
        _lblBuildingDescription.text = upText;

        // 4. 비용 기입 부위도 동일하게 겹침 방지 태그 주입
        _lblBuildingCost.text =
            $"<line-height=120%>[업그레이드 비용]\n개척 데이터: {nextLvData.frontierCost} | 목재: {nextLvData.woodCost} | 광물: {nextLvData.oreCost}</line-height>";

        Show(true);
    }

    // =========================================================================
    // 🍹 3. [상호작용 효과 사용 팝업 모드] (주점, 여관, 길드 전용 - 유니티 6 행간 보정 완료)
    // =========================================================================
    public void OpenInteraction(BuildingObject building)
    {
        if (building == null || building.Data == null) return;

        _currentMode = PopupMode.Interaction;
        _targetBuildingObject = building;
        _targetBuildingData = building.Data;

        var lvData = _targetBuildingData.GetLevelData(building.CurrentLevel);
        _lblBuildingName.text = $"{_targetBuildingData.DisplayName} 기능 이용";

        // 💥 [유니티 6 핵심 방어] 모든 텍스트의 시작과 끝을 <line-height=120%> 태그로 감싸서 겹침을 원천 차단합니다.
        string descText = "<line-height=120%>";

        // 분기 A: 길드 데이터 즉시 즉각 정산 안내
        if (_targetBuildingData.buildingType == BuildingType.Guild)
        {
            descText += "길드에 축적된 개척 보고서를 분석하여\n영지 데이터를 수집합니다.";
            descText += "</line-height>";

            _lblBuildingDescription.text = descText;
            _lblBuildingCost.text = $"<line-height=120%>[수령 보상]\n개척 데이터: +{lvData.effectValue} 수급 가능</line-height>";
        }
        // 분기 B: 주점(Tavern), 여관(Inn) 영웅 상태 기반 조건부 회복 안내
        else
        {
            BaseCharacter selectedHero = _characterSelector?.Selected;
            if (selectedHero == null)
            {
                descText += "<color=red>선택된 영웅이 없습니다.\n이용할 영웅을 먼저 선택해 주세요.</color>";
                descText += "</line-height>";

                _lblBuildingDescription.text = descText;
                _lblBuildingCost.text = "<line-height=120%>이용 불가</line-height>";
            }
            else
            {
                int preValue;

                // 🏡 여관 (Inn) : 체력 회복 분기
                if (_targetBuildingData.buildingType == BuildingType.Inn)
                {
                    preValue = Mathf.Min(selectedHero.Stat.current_Hp + lvData.effectValue, selectedHero.Stat.max_Hp);

                    descText += $"이용 영웅: <b>{selectedHero.Name}</b> " +
                                $"현재 상태 ➔ 체력: {selectedHero.Stat.current_Hp} / {selectedHero.Stat.max_Hp}\n\n" +
                                "[회복 피드백]\n" +
                                $"효과 적용 시 체력이 {lvData.effectValue}만큼 회복됩니다. " +
                                $"(예상 변경치 ➔ 체력: {selectedHero.Stat.current_Hp} ➔ {preValue})";
                }
                // 🍹 주점 (Tavern) : 광기 감소 분기
                else
                {
                    preValue = Mathf.Max(selectedHero.Insanity - lvData.effectValue, 0);

                    descText += $"이용 영웅: <b>{selectedHero.Name}</b> " +
                                $"현재 상태 ➔ 광기: {selectedHero.Insanity}\n\n" +
                                "[회복 피드백]\n" +
                                $"효과 적용 시 광기가 {lvData.effectValue}만큼 감소합니다. " +
                                $"(예상 변경치 ➔ 광기: {selectedHero.Insanity} ➔ {preValue})";
                }

                descText += "</line-height>";
                _lblBuildingDescription.text = descText;

                // 하단 비용 및 이용 횟수 레이블 겹침 예방
                int remainUses = lvData.maxUses - (building.Counter != null ? building.Counter.CurrentTurnUses : 0);
                _lblBuildingCost.text =
                    $"<line-height=120%>[이용 제한]\n이번 턴 남은 횟수: {remainUses} / {lvData.maxUses}회</line-height>";
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
    
    // =========================================================================
    // 🔄 상호작용 UI 전용 실시간 리프레시 라인
    // 캐릭터 셀렉터 변경 시 대장간 갱신 플로우와 함께 동적 연동됩니다.
    // =========================================================================
    public void RefreshCurrentInteraction()
    {
        // 💥 현재 UI가 열려있지 않거나, 상호작용 모드가 아니라면 연산을 완전히 스킵합니다!
        if (!IsAnyPopupOpen || _currentMode != PopupMode.Interaction) return;

        // 타겟 건물 데이터가 날아갔다면 방어 처리
        if (_targetBuildingObject == null) return;
        
        // 기존에 정밀하게 설계해 둔 상호작용 오픈 로직을 그대로 재호출하여 
        // 바뀐 영웅(SelectedHero) 데이터로 텍스트만 깔끔하게 새로고침합니다.
        OpenInteraction(_targetBuildingObject);
    }

    public void Show(bool isShow) => _root.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
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