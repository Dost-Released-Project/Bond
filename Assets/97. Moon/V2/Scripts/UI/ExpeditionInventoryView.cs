using System;
using System.Collections.Generic;
using Bond.Expedition;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using Random = UnityEngine.Random;

public class ExpeditionInventoryView : MonoBehaviour
{
    private ExpeditionInventory _expeditionInventory;
    [Inject] private InventoryTransferService _transferService;
    [Inject] private CharacterItemService _itemService;
    [Inject] private ExpeditionPayload _payload;
    
    private VisualElement _slotContainer, _localGhost;
    private List<VisualElement> _slots = new();

    private void Start()
    {
        _expeditionInventory = _payload.Supplies;
        var doc = GetComponent<UIDocument>().rootVisualElement;
        _slotContainer = doc.Q<VisualElement>("expedition-container");
        
        _localGhost = new VisualElement();
        _localGhost.style.position = Position.Absolute;
        _localGhost.style.width = _localGhost.style.height = 50;
        _localGhost.style.visibility = Visibility.Hidden;
        _localGhost.pickingMode = PickingMode.Ignore;
        doc.Add(_localGhost);

        // [추가] 최상위 영역 바깥(슬롯 외 구역)에 드롭했을 때 아이템 파괴(버리기) 로직
        doc.RegisterCallback<PointerUpEvent>(evt => {
            if (_transferService.IsDragging) 
            {
                var sourceInv = _transferService.CurrentSourceInventory;
                int sourceIdx = _transferService.CurrentDraggingIndex;
                if (sourceInv != null && sourceIdx != -1) 
                {
                    var slotData = sourceInv.GetSlot(sourceIdx);
                    if (!slotData.IsEmpty) 
                    {
                        Debug.Log($"[아이템 파괴] 영역 밖에 드롭하여 {slotData.item.itemName}을(를) 버렸습니다.");
                        sourceInv.ClearSlot(sourceIdx);
                    }
                }
                _transferService.ResetDrag();
            }
            else if (_transferService.IsDraggingFromEquipment)
            {
                // 장비 슬롯에서 꺼내서 밖에 버렸을 때 처리
                var hero = AdminTestTool.testHero;
                if (hero != null)
                {
                    _itemService.DiscardEquipment(hero, _transferService.SourceEquipmentSlotIndex);
                }
                _transferService.ResetEquipmentDrag();
            }
        }, TrickleDown.NoTrickleDown); // 버블링 단계에서 최상위 도달 시 처리

        _expeditionInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    public void ToggleWindow()
    {
        _slotContainer.style.display = (_slotContainer.style.display == DisplayStyle.None) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Update()
    {
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            _payload.Supplies.AddItemAuto(Resources.Load<BaseItem>($"Data/Items/Consumables/070{Random.Range(0,5)}0000"), 2);
        }

        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            _payload.Supplies.ClearSlot(0);
        }

        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleWindow();
        }
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
                
                if (evt.button == 0) { // 좌클릭 드래그 시작
                    _transferService.StartDrag(_expeditionInventory, index);
                }
                else if (evt.button == 1) { // 우클릭 자동 사용/장착
                    if (data.item.category == ItemCategory.Accessories) _itemService.AutoEquip(_expeditionInventory, index);
                    else if (data.item.category == ItemCategory.Consume) _itemService.UseItem(AdminTestTool.testHero, _expeditionInventory, index);
                }
            });

            slot.RegisterCallback<PointerUpEvent>(evt => {
                if (_transferService.IsDragging) {
                    _transferService.ExecuteDragDrop(_transferService.CurrentSourceInventory, _transferService.CurrentDraggingIndex, _expeditionInventory, index);
                    _transferService.ResetDrag();
                    evt.StopPropagation(); // 최상위 버리기 이벤트로 전파되는 것을 차단
                }
                else if (_transferService.IsDraggingFromEquipment) {
                    // 장비 슬롯에서 인벤토리 슬롯으로 드래그 앤 드롭했을 때 (해제 혹은 스왑)
                    var hero = AdminTestTool.testHero;
                    if (hero != null) {
                        _itemService.UnequipToInventorySlot(hero, _transferService.SourceEquipmentSlotIndex, _expeditionInventory, index);
                    }
                    _transferService.ResetEquipmentDrag();
                    evt.StopPropagation(); // 전파 차단
                }
            });

            _slotContainer.Add(slot);
            _slots.Add(slot);
            
            _payload.SetSuplies(_expeditionInventory);
        }
    }
}