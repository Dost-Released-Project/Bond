using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using System.Collections.Generic;
using System.Linq;

public class ConstructionUI : MonoBehaviour
{
    [SerializeField] private List<BuildingData> buildingTemplates; // 인스펙터에서 6종 데이터를 넣어주세요.

    private VisualElement _root;
    private SettlementManager _settlementManager;
    private int _targetSlotIndex;

    [Inject]
    public void Construct(SettlementManager sm) => _settlementManager = sm;

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
        // 템플릿 리스트에서 해당 타입의 데이터를 찾음
        var data = buildingTemplates.FirstOrDefault(d => d.buildingType == type);
        
        if (data != null)
        {
            _settlementManager.BuildInSlot(_targetSlotIndex, data);
            Show(false); // 건설 시도 후 창 닫기
        }
        else
        {
            Debug.LogError($"{type} 타입의 BuildingData가 UI 템플릿 리스트에 없습니다!");
        }
    }

    private void Show(bool isShow) => _root.style.display = isShow ? DisplayStyle.Flex : DisplayStyle.None;
}