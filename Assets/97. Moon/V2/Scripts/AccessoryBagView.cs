using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class AccessoryBagView : MonoBehaviour
{
    private ITotalInventory _totalInventory;
    private CharacterEquipService _equipService;
    private VisualElement _grid;
    private List<VisualElement> _uiSlots = new();
    private List<int> _mappedIndices = new();

    [Inject]
    public void Construct(ITotalInventory total, CharacterEquipService equip) 
    { 
        _totalInventory = total; 
        _equipService = equip; 
    }

    private void Start()
    {
        _grid = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("accessory-grid");
        
        // 데이터 변경 구독
        _totalInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    // 슬롯이 모자랄 경우 동적으로 생성하는 메서드
    private void SyncSlots(int requiredCount)
    {
        while (_uiSlots.Count < requiredCount)
        {
            int uiIdx = _uiSlots.Count; // 현재 리스트 개수가 곧 새 슬롯의 인덱스
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");

            // 이벤트 등록 (생성 시 한 번만 수행)
            slot.RegisterCallback<PointerDownEvent>(e => {
                if (uiIdx < _mappedIndices.Count) {
                    int actualInvIdx = _mappedIndices[uiIdx];
                    if (e.button == 0) { 
                        InventoryView.CurrentDraggingIndex = actualInvIdx;
                        InventoryView.CurrentSourceInventory = _totalInventory;
                    } 
                    else if (e.button == 1) { 
                        _equipService.AutoEquip(_totalInventory, actualInvIdx);
                        // 데이터가 바뀌었으므로 OnChanged에 의해 RefreshUI가 호출되지만, 
                        // 즉각적인 피드백을 위해 한 번 더 호출해도 무방합니다.
                    }
                }
            });

            _grid.Add(slot);
            _uiSlots.Add(slot);
        }
    }

    public void RefreshUI()
    {
        // 1. 장신구 아이템이 들어있는 실제 인덱스들만 필터링
        _mappedIndices = _totalInventory.GetFilteredIndices("", ItemCategory.Accessories).ToList();

        // 2. 필터링된 아이템 개수에 맞춰 슬롯 생성 (빈 칸은 안 보이게 할 것이므로 개수만큼만 생성/준비)
        SyncSlots(_mappedIndices.Count);

        // 3. UI 갱신
        for (int i = 0; i < _uiSlots.Count; i++)
        {
            var slotVisual = _uiSlots[i];
            slotVisual.Clear();

            // 요청사항: 장신구가 들어있는 칸만 보이게 처리
            if (i < _mappedIndices.Count)
            {
                slotVisual.style.display = DisplayStyle.Flex;
                var data = _totalInventory.GetSlot(_mappedIndices[i]);

                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(data.item.icon);
                icon.pickingMode = PickingMode.Ignore; // 아이콘이 클릭을 가로막지 않게 설정
                icon.style.width = icon.style.height = Length.Percent(100);
                
                slotVisual.Add(icon);
                slotVisual.pickingMode = PickingMode.Position;
            }
            else
            {
                // 데이터 개수를 초과하는 슬롯들은 숨김 처리 (빈 칸 안 보이게)
                slotVisual.style.display = DisplayStyle.None;
            }
        }
    }

    private void OnDestroy()
    {
        if (_totalInventory != null) _totalInventory.OnChanged -= RefreshUI;
    }
}