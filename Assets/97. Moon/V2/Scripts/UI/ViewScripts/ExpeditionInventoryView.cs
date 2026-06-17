using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bond.Expedition;
using Bond.Persistence;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using Random = UnityEngine.Random;

public class ExpeditionInventoryView : MonoBehaviour
{
    [Inject] private InventoryTransferService _transferService;
    [Inject] private CharacterItemService _itemService;
    [Inject] private ExpeditionPayload _payload;
    [Inject] private CharacterSelector _characterSeletor;
    
    private VisualElement _slotContainer;
    private List<VisualElement> _slots = new();
    private VisualElement _root;
    private VisualElement _tooltip; // 상세 정보를 띄울 최상위 오버레이 레이어

    private void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _slotContainer = _root.Q<VisualElement>("expedition-container");

        // [툴팁 초기화]: 마우스를 올렸을 때 화면 클리핑(잘림) 없이 자유롭게 떠다닐 수 있는 정석 오버레이 요소 배치
        _tooltip = new VisualElement { style = { position = Position.Absolute, visibility = Visibility.Hidden } };
        _tooltip.pickingMode = PickingMode.Ignore; // 툴팁 자체가 마우스를 가로막아 끊기는 버그 방어
        _tooltip.AddToClassList("inventory-tooltip"); // 공용 USS 스타일 스킨 입히기
        _root.Add(_tooltip);
        
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
                    //var hero = AdminTestTool.testHero;
                    var hero = _characterSeletor.Selected;
                    if (hero != null)
                    {
                        _itemService.DiscardEquipment(hero, _transferService.SourceEquipmentSlotIndex);
                    }
                }
                _transferService.ResetEquipmentDrag();
            }
        }, TrickleDown.NoTrickleDown); // 슬롯에서 이벤트를 먹지 않았을 때(버블링 최종 단계) 실행

        _payload.Supplies.OnChanged += RefreshUI;
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
            _payload.AddReward(100,10,10);
        }
    }

    public void RefreshUI()
    {
        SyncSlots();
        for (int i = 0; i < _slots.Count; i++)
        {
            var data = _payload.Supplies.GetSlot(i);
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
        while (_slots.Count < _payload.Supplies.Capacity)
        {
            int index = _slots.Count;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");

            slot.RegisterCallback<PointerDownEvent>(evt => {
                var data = _payload.Supplies.GetSlot(index);
                if (data.IsEmpty) return;
                
                if (evt.button == 0) { // 좌클릭 드래그 시작
                    HideTooltip();
                    _transferService.StartDrag(_payload.Supplies, index);
                }
                else if (evt.button == 1) { // 우클릭 자동 사용/장착
                    if (data.item.category == ItemCategory.Accessories) _itemService.AutoEquip(_payload.Supplies, index);
                    //else if (data.item.category == ItemCategory.Consume) _itemService.UseItem(AdminTestTool.testHero, _payload.Supplies, index);
                    else if (data.item.category == ItemCategory.Consume) _itemService.UseItem(_characterSeletor.Selected, _payload.Supplies, index);
                }
            });

            slot.RegisterCallback<PointerUpEvent>(evt => {
                if (_transferService.IsDragging) {
                    _transferService.ExecuteDragDrop(_transferService.CurrentSourceInventory, _transferService.CurrentDraggingIndex, _payload.Supplies, index);
                    _transferService.ResetDrag();
                    evt.StopPropagation(); // 정상 슬롯에 안착했으므로 최상위 '영역 밖 버리기'로 이벤트가 흐르지 않게 차단
                }
                else if (_transferService.IsDraggingFromEquipment)
                {
                    //var hero = AdminTestTool.testHero;
                    var hero = _characterSeletor.Selected;
                    if (hero != null) {
                        _itemService.UnequipToInventorySlot(hero, _transferService.SourceEquipmentSlotIndex, _payload.Supplies, index);
                    }
                    _transferService.ResetEquipmentDrag();
                    evt.StopPropagation(); // 차단
                }
            });
            
            // [툴팁 연동]: 마우스가 슬롯에 진입하거나 이탈할 때 실시간으로 툴팁을 조율합니다.
            slot.RegisterCallback<MouseEnterEvent>(evt => {
                var data = _payload.Supplies.GetSlot(index);
                ShowTooltip(data, evt.mousePosition);
            });

            slot.RegisterCallback<MouseLeaveEvent>(evt => {
                HideTooltip();
            });

            _slotContainer.Add(slot);
            _slots.Add(slot);
            
            _payload.SetSuplies(_payload.Supplies);
        }
    }
    
    /// <summary>마우스 좌표를 수신하여 아이템 상세 정보를 오버레이에 출력하고 이탈을 방지합니다.</summary>
    private void ShowTooltip(InventorySlot slot, Vector2 position)
    {
        // 슬롯이 비어있거나 현재 마우스로 무언가를 드래그 중일 때는 가독성을 위해 툴팁 출력을 거부합니다.
        if (slot.IsEmpty || _transferService.CurrentSourceInventory != null ||
            _transferService.IsEquipmentDragging) return;

        _tooltip.Clear();

        // 타이틀 라벨 생성 및 클래스 부여
        var title = new Label(slot.item.DisplayName);
        title.AddToClassList("tooltip-title");
        _tooltip.Add(title);

        // 공용 텍스트 필러
        AddTooltipLabel($"{(string.IsNullOrEmpty(slot.item.Description) ? "내용 없음" : slot.item.Description)}");
        AddTooltipLabel($"보유량: {slot.quantity} / {slot.item.expeditionSlotMax}");

        // 아이템 종류별 파싱 분기
        if (slot.item is ConsumableItem con && con.healValue != 0)
            AddTooltipLabel($"회복량: <color=#00FF00>{con.healValue}</color>");

        if (slot.item is AccessoryItem acc && acc.specialEffects.Count > 0)
        {
            AddTooltipLabel("\n[장착 효과]");
            foreach (var effect in acc.specialEffects)
            {
                var valueMode = effect.mode == ModifierMode.Flat ? $"{effect.value} 증가" : $"{effect.value:P1} 증가";
                AddTooltipLabel($"- {effect.name}: {effect.type} + {valueMode}");
            }
        }

        // 🖥️ [툴팁 스크린 이탈 방지 배리어] 
        float tooltipWidth = 280f;
        float tooltipHeight = 220f;

        // 가로 화면 벽 뚫기 방어 연산
        float finalX = position.x + 20f;
        if (finalX + tooltipWidth > Screen.width)
        {
            finalX = position.x - tooltipWidth - 20f;
        }

        // 세로 화면 바닥 뚫기 방어 연산
        float finalY = position.y + 20f;
        if (finalY + tooltipHeight > Screen.height)
        {
            finalY = position.y - tooltipHeight - 20f;
        }

        // 극단적인 외곽 스크린 보정 최소값 배리어
        if (finalX < 10f) finalX = 10f;
        if (finalY < 10f) finalY = 10f;

        _tooltip.style.left = finalX;
        _tooltip.style.top = finalY;
        _tooltip.style.visibility = Visibility.Visible;
        _tooltip.BringToFront();
    }

    private void AddTooltipLabel(string text)
    {
        var label = new Label(text);
        label.AddToClassList("tooltip-text");
        _tooltip.Add(label);
    }

    private void HideTooltip()
    {
        if (_tooltip != null) _tooltip.style.visibility = Visibility.Hidden;
    }
}