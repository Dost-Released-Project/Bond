using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using System.Collections.Generic;
using System.Linq;
using UnityEngine.AddressableAssets;

public class ConstructionUI : MonoBehaviour
{
    // 리스트 대신 데이터베이스 SO 자체를 들고 있습니다.
    private BuildingDataBaseSO _buildingDB;

    private VisualElement _root;
    private SettlementManager _settlementManager;
    private int _targetSlotIndex;

    [Inject]
    public void Construct(SettlementManager sm) => _settlementManager = sm;
    
    private async void Awake()
    {
        var handle = Addressables.LoadAssetAsync<BuildingDataBaseSO>("BuildingDataBase");
        _buildingDB = await handle.Task;
    }

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.style.display = DisplayStyle.None;

        // 버튼 이름과 BuildingType을 매칭해서 이벤트 등록
        BindButton("btn-inn", BuildingType.Inn);
        BindButton("btn-tavern", BuildingType.Tavern);
        BindButton("btn-smithy", BuildingType.Smithy);
        BindButton("btn-guild", BuildingType.Guild);
        BindButton("btn-storage", BuildingType.Storage);
        BindButton("btn-carriage", BuildingType.Carriage);
        BindButton("btn-supply", BuildingType.Supply);
        
        _root.Q<Button>("btn-close").clicked += () => Show(false);
    }

    private void BindButton(string btnName, BuildingType type)
    {
        var btn = _root.Q<Button>(btnName);
        if (btn != null)
        {
            btn.clicked += () => Build(type);
        }
    }

    public void Open(int slotIndex)
    {
        _targetSlotIndex = slotIndex;
        Show(true);
    }

    private void Build(BuildingType type)
    {
        if (_buildingDB == null) return;

        // 리스트 전체를 가져오지 않고, DB에게 "이 타입인 거 하나 찾아줘"라고 요청
        var data = _buildingDB.FindSO<BuildingData>(d => d.buildingType == type);
    
        if (data != null)
        {
            _settlementManager.BuildInSlot(_targetSlotIndex, data);
            Show(false);
        }
    }

    private void Show(bool isShow) => _root.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
}