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
    private SettlementManager _settlementManager;
    
    private int _targetSlotIndex;
    private BuildingData _targetBuildingData; // 현재 슬롯에 매핑된 건물 데이터 캐싱

    // 💥 [신규 UI Toolkit 레이블 명세]
    private Label _lblBuildingName;
    private Label _lblBuildingDescription;
    private Label _lblBuildingCost;
    private Button _btnConfirm;

    [Inject]
    public void Construct(SettlementManager sm) => _settlementManager = sm;
    
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

        // 🎯 [기존 7개 버튼 철거] 단일 상세 보기용 UI 엘리먼트 쿼리 매핑
        _lblBuildingName = _root.Q<Label>("construction-name");
        _lblBuildingDescription = _root.Q<Label>("construction-desc");
        _lblBuildingCost = _root.Q<Label>("construction-cost");
        
        _btnConfirm = _root.Q<Button>("btn-confirm"); // [건설한다] 버튼
        var btnCancel = _root.Q<Button>("btn-cancel");   // [취소한다] 버튼

        if (_btnConfirm != null) _btnConfirm.clicked += ExecuteBuild;
        if (btnCancel != null) btnCancel.clicked += () => Show(false);
        
        // 닫기 버튼 예외 방어
        var btnClose = _root.Q<Button>("btn-close");
        if (btnClose != null) btnClose.clicked += () => Show(false);
    }

    // 💥 [기능 변경] 슬롯이 열릴 때 지을 건물 종류를 받아와서 화면을 동적으로 가공합니다.
    public void Open(int slotIndex, BuildingType type)
    {
        if (_buildingDB == null) return;

        _targetSlotIndex = slotIndex;
        
        // DB에서 슬롯 지정 전용 건물 SO 추적
        _targetBuildingData = _buildingDB.FindSO<BuildingData>(d => d.buildingType == type);

        if (_targetBuildingData == null)
        {
            Debug.LogError($"[UI 에러] DB에서 {type} 타입을 가진 BuildingData를 찾을 수 없습니다.");
            return;
        }

        // 📊 UI 텍스트 동적 매핑 연산
        _lblBuildingName.text = _targetBuildingData.DisplayName;
        _lblBuildingDescription.text = string.IsNullOrEmpty(_targetBuildingData.Description) 
            ? "영지의 특수 기능을 담당하는 건물입니다." 
            : _targetBuildingData.Description;

        // 1레벨(최초 건설) 기준 소모 자원 파싱
        var firstLevelData = _targetBuildingData.GetLevelData(1);
        if (firstLevelData.level != 0)
        {
            // (※ 프로젝트의 실제 자원 비용 수식 변수명에 맞게 조정해 주시면 됩니다.)
            // 예시: 비용 데이터 테이블 구조 파싱
            _lblBuildingCost.text = $"필요 데이터: {firstLevelData.frontierCost} | 필요 목재: {firstLevelData.woodCost} | 필요 광물: {firstLevelData.oreCost}";
        }

        Show(true);
    }

    // [건설한다] 최종 승인 버튼 클릭 시 발동
    private void ExecuteBuild()
    {
        if (_targetBuildingData != null)
        {
            _settlementManager.BuildInSlot(_targetSlotIndex, _targetBuildingData);
            Show(false);
        }
    }

    private void Show(bool isShow) => _root.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
    
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
                    if (data != null)
                    {
                        _settlementManager.LoadBuilding(b.slotIndex, data, b.level);
                    }
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