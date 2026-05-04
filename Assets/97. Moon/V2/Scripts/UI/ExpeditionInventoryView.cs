using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class ExpeditionInventoryView : MonoBehaviour
{
    private IExpeditionInventory _expeditionInventory;
    private InventoryTransferService _transferService;
    private CharacterItemService _itemService;
    private InventoryUIService _uiService;
    
    private VisualElement _slotContainer, _localGhost;
    private List<VisualElement> _slots = new();
    private bool IsWindowActive = true;

    [Inject]
    public void Construct(IExpeditionInventory inventory, InventoryTransferService transfer, 
        CharacterItemService itemService, InventoryUIService uiService)
    {
        _expeditionInventory = inventory; _transferService = transfer;
        _itemService = itemService; _uiService = uiService;
    }

    private void Start()
    {
        var doc = GetComponent<UIDocument>().rootVisualElement;
        _slotContainer = doc.Q<VisualElement>("expedition-container");
        
        // 탐사 전용 고스트 (UIService가 사용할 수 있게 설정)
        _localGhost = new VisualElement();
        _localGhost.style.position = Position.Absolute;
        _localGhost.style.width = _localGhost.style.height = 50;
        _localGhost.style.visibility = Visibility.Hidden;
        _localGhost.pickingMode = PickingMode.Ignore;
        doc.Add(_localGhost);

        _expeditionInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    public void ToggleWindow()
    {
        IsWindowActive = !IsWindowActive;
        _slotContainer.style.display = IsWindowActive ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public void RefreshUI()
    {
        SyncSlots();
        for (int i = 0; i < _slots.Count; i++)
        {
            var data = _expeditionInventory.GetSlot(i);
            _slots[i].Clear();
            if (!data.IsEmpty)
            {
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(data.item.icon);
                icon.style.width = icon.style.height = Length.Percent(100);
                icon.pickingMode = PickingMode.Ignore;
                _slots[i].Add(icon);
            }
        }
    }

    private void SyncSlots()
    {
        while (_slots.Count < _expeditionInventory.Capacity)
        {
            int index = _slots.Count;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");

            slot.RegisterCallback<PointerDownEvent>(evt => {
                var data = _expeditionInventory.GetSlot(index);
                if (data.IsEmpty) return;

                if (evt.button == 0) {
                    _uiService.StartDrag(_expeditionInventory, index, data.item.icon, _localGhost, evt.position, new Vector2(25, 25));
                }
                else if (evt.button == 1) {
                    if (data.item.category == ItemCategory.Accessories) _itemService.AutoEquip(_expeditionInventory, index);
                    else if (data.item.category == ItemCategory.Consume) _itemService.UseItem(AdminTestTool.testHero, _expeditionInventory, index);
                }
            });

            slot.RegisterCallback<PointerMoveEvent>(evt => {
                if (_uiService.CurrentSourceInventory != null) _uiService.UpdateGhostPosition(evt.position, new Vector2(25, 25));
            });

            slot.RegisterCallback<PointerUpEvent>(evt => {
                if (_uiService.CurrentSourceInventory != null) {
                    _transferService.ExecuteDragDrop(_uiService.CurrentSourceInventory, _uiService.CurrentDraggingIndex, _expeditionInventory, index);
                    _uiService.ResetDrag();
                }
            });

            _slotContainer.Add(slot);
            _slots.Add(slot);
        }
    }
}