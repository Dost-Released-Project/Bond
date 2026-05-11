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
    
    private VisualElement _slotContainer;
    private List<VisualElement> _slots = new();
    private VisualElement _root;

    private void Start()
    {
        _expeditionInventory = _payload.Supplies;
        _root = GetComponent<UIDocument>().rootVisualElement;
        _slotContainer = _root.Q<VisualElement>("expedition-container");

        // [개선] 마우스 커서가 하얀 영역(_slotContainer)을 완전히 벗어났을 때만 버리기 판정
        _root.RegisterCallback<PointerUpEvent>(evt => {
            if (_transferService.IsDragging) 
            {
                // 실제 마우스 커서 좌표가 하얀색 인벤토리 컨테이너 바운드 외부에 있을 때만 삭제
                if (!_slotContainer.worldBound.Contains(evt.position))
                {
                    var sourceInv = _transferService.CurrentSourceInventory;
                    int sourceIdx = _transferService.CurrentDraggingIndex;
                    if (sourceInv != null && sourceIdx != -1) 
                    {
                        var slotData = sourceInv.GetSlot(sourceIdx);
                        if (!slotData.IsEmpty) 
                        {
                            Debug.Log($"[아이템 파괴] 인벤토리 영역 밖에 드롭하여 {slotData.item.itemName}을(를) 버렸습니다.");
                            sourceInv.ClearSlot(sourceIdx);
                        }
                    }
                }
                else
                {
                    // 하얀 공간 안에서 슬롯이 아닌 곳에 놓았다면 아무 일도 하지 않고 복구(제자리)
                    Debug.Log("[드래그 취소] 인벤토리 내부 빈 공간에 드롭되어 제자리로 복구됩니다.");
                }
                _transferService.ResetDrag();
            }
            else if (_transferService.IsDraggingFromEquipment)
            {
                // 장비 슬롯에서 마우스를 떼었을 때도 하얀 영역 밖일 때만 파괴
                if (!_slotContainer.worldBound.Contains(evt.position))
                {
                    var hero = AdminTestTool.testHero;
                    if (hero != null)
                    {
                        _itemService.DiscardEquipment(hero, _transferService.SourceEquipmentSlotIndex);
                    }
                }
                _transferService.ResetEquipmentDrag();
            }
        }, TrickleDown.NoTrickleDown); // 슬롯에서 이벤트를 먹지 않았을 때(버블링 최종 단계) 실행

        _expeditionInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    public void ToggleWindow()
    {
        _root.style.display = (_root.style.display == DisplayStyle.None) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Update()
    {
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
                    evt.StopPropagation(); // 정상 슬롯에 안착했으므로 최상위 '영역 밖 버리기'로 이벤트가 흐르지 않게 차단
                }
                else if (_transferService.IsDraggingFromEquipment) {
                    var hero = AdminTestTool.testHero;
                    if (hero != null) {
                        _itemService.UnequipToInventorySlot(hero, _transferService.SourceEquipmentSlotIndex, _expeditionInventory, index);
                    }
                    _transferService.ResetEquipmentDrag();
                    evt.StopPropagation(); // 차단
                }
            });

            _slotContainer.Add(slot);
            _slots.Add(slot);
            
            _payload.SetSuplies(_expeditionInventory);
        }
    }
}