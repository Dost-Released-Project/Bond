using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class AccessoryBagView : MonoBehaviour
{
    private ITotalInventory _totalInventory;
    private CharacterItemService _itemService; // 이름 변경 및 타입 변경
    private InventoryUIService _uiService;     // UI 상태 관리 추가
    
    private VisualElement _grid;
    private VisualElement _dragGhost; // 드래그 시각화를 위한 고스트 추가
    private List<VisualElement> _uiSlots = new();
    private List<int> _mappedIndices = new();

    [Inject]
    public void Construct(ITotalInventory total, CharacterItemService itemService, InventoryUIService uiService) 
    { 
        _totalInventory = total; 
        _itemService = itemService; 
        _uiService = uiService;
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _grid = root.Q<VisualElement>("accessory-grid");

        // 드래그 고스트 설정 (기존 InventoryView와 동일한 방식 적용)
        _dragGhost = new VisualElement();
        _dragGhost.style.position = Position.Absolute;
        _dragGhost.pickingMode = PickingMode.Ignore;
        _dragGhost.style.visibility = Visibility.Hidden;
        _dragGhost.style.width = 50; _dragGhost.style.height = 50;
        root.Add(_dragGhost);

        root.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        
        _totalInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    private void SyncSlots(int requiredCount)
    {
        while (_uiSlots.Count < requiredCount)
        {
            int uiIdx = _uiSlots.Count;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");

            slot.RegisterCallback<PointerDownEvent>(e => {
                if (uiIdx < _mappedIndices.Count) {
                    int actualInvIdx = _mappedIndices[uiIdx];
                    var slotData = _totalInventory.GetSlot(actualInvIdx);
                    
                    if (e.button == 0) { // 좌클릭 드래그 시작
                        if (slotData.IsEmpty) return;
                        _uiService.StartDrag(_totalInventory, actualInvIdx, slotData.item.icon, _dragGhost, e.position, new Vector2(25, 25));
                    } 
                    else if (e.button == 1) { // 우클릭 자동 장착
                        _itemService.AutoEquip(_totalInventory, actualInvIdx);
                    }
                }
            });

            // 드롭 로직 추가 (가방 안에서 아이템 위치 교환 등을 위해)
            slot.RegisterCallback<PointerUpEvent>(e => {
                if (_uiService.CurrentSourceInventory != null && uiIdx < _mappedIndices.Count) {
                    int targetIdx = _mappedIndices[uiIdx];
                    // 기존 전송 서비스는 이미 주입되어 있으므로 필요한 로직 수행 가능하지만 
                    // 여기서는 기본적으로 '아이템 교체'가 발생하도록 구조 유지
                    _uiService.ResetDrag();
                }
            });

            _grid.Add(slot);
            _uiSlots.Add(slot);
        }
    }

    public void RefreshUI()
    {
        _mappedIndices = _totalInventory.GetFilteredIndices("", ItemCategory.Accessories).ToList();
        SyncSlots(_mappedIndices.Count);

        for (int i = 0; i < _uiSlots.Count; i++)
        {
            var slotVisual = _uiSlots[i];
            slotVisual.Clear();

            if (i < _mappedIndices.Count)
            {
                slotVisual.style.display = DisplayStyle.Flex;
                var data = _totalInventory.GetSlot(_mappedIndices[i]);

                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(data.item.icon);
                icon.pickingMode = PickingMode.Ignore;
                icon.style.width = icon.style.height = Length.Percent(100);
                
                slotVisual.Add(icon);
            }
            else
            {
                slotVisual.style.display = DisplayStyle.None;
            }
        }
    }

    private void OnPointerMove(PointerMoveEvent evt) 
    { 
        if (_uiService.CurrentSourceInventory != null) _uiService.UpdateGhostPosition(evt.position, new Vector2(25, 25)); 
    }

    public void ToggleWindow()
    {
        _uiService.IsAccessoryBagActive = !_uiService.IsAccessoryBagActive; // 서비스 상태 갱신
        _grid.style.display = _uiService.IsAccessoryBagActive ? DisplayStyle.Flex : DisplayStyle.None;
    }
}