using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class AccessoryBagView : MonoBehaviour
{
    private ITotalInventory _totalInventory;
    private CharacterItemService _itemService; 
    private InventoryTransferService _transferService; // 주입 추가
    
    private VisualElement _grid;
    private VisualElement _dragGhost; 
    private List<VisualElement> _uiSlots = new();
    private List<int> _mappedIndices = new();

    [Inject]
    public void Construct(ITotalInventory total, CharacterItemService itemService, InventoryTransferService transferService, InventoryUIService uiService) 
    { 
        _totalInventory = total; 
        _itemService = itemService; 
        _transferService = transferService;
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _grid = root.Q<VisualElement>("accessory-grid");

        _dragGhost = new VisualElement();
        _dragGhost.style.position = Position.Absolute;
        _dragGhost.pickingMode = PickingMode.Ignore;
        _dragGhost.style.visibility = Visibility.Hidden;
        _dragGhost.style.width = 50; _dragGhost.style.height = 50;
        root.Add(_dragGhost);
        
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
                        _transferService.StartDrag(_totalInventory, actualInvIdx);
                    } 
                    else if (e.button == 1) { // 우클릭 자동 장착
                        _itemService.AutoEquip(_totalInventory, actualInvIdx);
                    }
                }
            });

            slot.RegisterCallback<PointerUpEvent>(e => {
                if (_transferService.IsDragging && uiIdx < _mappedIndices.Count) {
                    int targetIdx = _mappedIndices[uiIdx];
                    // 가방 내부 스왑 진행
                    _transferService.ExecuteDragDrop(_transferService.CurrentSourceInventory, _transferService.CurrentDraggingIndex, _totalInventory, targetIdx);
                    _transferService.ResetDrag();
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

    public void ToggleWindow()
    { 
        _grid.style.display = (_grid.style.display == DisplayStyle.None) ? DisplayStyle.Flex : DisplayStyle.None;
    }
}