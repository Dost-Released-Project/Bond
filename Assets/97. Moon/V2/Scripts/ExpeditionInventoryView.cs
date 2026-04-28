using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class ExpeditionInventoryView : MonoBehaviour
{
    private IExpeditionInventory _expeditionInventory;
    private InventoryTransferService _transferService;
    private CharacterEquipService _equipService;
    
    private VisualElement _slotContainer;
    private VisualElement _localGhost; // 독립 고스트
    private List<VisualElement> _slots = new();

    private bool IsWindowActive = true;

    [Inject]
    public void Construct(IExpeditionInventory inventory, InventoryTransferService transfer, CharacterEquipService equip)
    {
        _expeditionInventory = inventory;
        _transferService = transfer;
        _equipService = equip;
    }

    private void Start()
    {
        var doc = GetComponent<UIDocument>().rootVisualElement;
        _slotContainer = doc.Q<VisualElement>("expedition-container");
        
        // 독립 고스트 생성
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

                if (evt.button == 0) { // 드래그 시작
                    InventoryView.CurrentDraggingIndex = index;
                    InventoryView.CurrentSourceInventory = _expeditionInventory;
                    _localGhost.style.backgroundImage = new StyleBackground(data.item.icon);
                    _localGhost.style.visibility = Visibility.Visible;
                    UpdateGhost(evt.position);
                }
                else if (evt.button == 1) { // 우클릭 분기
                    if (data.item.category == ItemCategory.Accessories) _equipService.AutoEquip(_expeditionInventory, index);
                    else if (data.item.category == ItemCategory.Consume) UseItemInExpedition(index);
                }
            });

            slot.RegisterCallback<PointerMoveEvent>(evt => {
                if (_localGhost.style.visibility == Visibility.Visible) UpdateGhost(evt.position);
            });

            slot.RegisterCallback<PointerUpEvent>(evt => {
                _localGhost.style.visibility = Visibility.Hidden;
                if (InventoryView.CurrentSourceInventory != null) {
                    _transferService.ExecuteDragDrop(InventoryView.CurrentSourceInventory, InventoryView.CurrentDraggingIndex, _expeditionInventory, index);
                    InventoryView.ResetDraggingState();
                }
            });

            _slotContainer.Add(slot);
            _slots.Add(slot);
        }
    }

    private void UpdateGhost(Vector2 pos)
    {
        _localGhost.style.left = pos.x - 25;
        _localGhost.style.top = pos.y - 25;
    }

    private void UseItemInExpedition(int index) // 복구된 소모품 사용 로직
    {
        var slot = _expeditionInventory.GetSlot(index);
        if (slot.IsEmpty || slot.item.category != ItemCategory.Consume) return;
        if (AdminTestTool.testHero != null)
        {
            slot.item.Use(AdminTestTool.testHero);
            _expeditionInventory.RemoveFromSlot(index, 1);
        }
    }
}