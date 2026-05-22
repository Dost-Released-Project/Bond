using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class AccessoryBagView : MonoBehaviour
{
    private ITotalInventory _totalInventory;
    private CharacterItemService _itemService; 
    private InventoryTransferService _transferService;
    [Inject] private CharacterSelector _selector;
    
    private VisualElement _grid, _tooltip;
    private List<VisualElement> _uiSlots = new();
    private List<int> _mappedIndices = new();

    [Inject]
    public void Construct(ITotalInventory total, CharacterItemService itemService, InventoryTransferService transferService) 
    { 
        _totalInventory = total; _itemService = itemService; _transferService = transferService;
    }

    private void Start()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        _grid = root.Q<VisualElement>("accessory-grid");

        // 툴팁 초기화
        _tooltip = new VisualElement();
        _tooltip.AddToClassList("inventory-tooltip");
        _tooltip.pickingMode = PickingMode.Ignore;
        root.Add(_tooltip);
        
        // [정밀 튜닝] 가방 컨테이너 전체에 Drop 이벤트 배치 및 타겟 검증 분기
        _grid.RegisterCallback<PointerUpEvent>(e => {
            if (_transferService.IsEquipmentDragging)
            {
                // [핵심] 마우스를 놓은 최종 대상(e.target)이 리스트 아이템 슬롯이 아닐 때만 실행합니다.
                // 즉, 가방의 빈 여백 영역이나 스크롤 뷰 컨테이너 자체에 드롭했을 때만 '순수 해제'를 수행합니다.
                if (e.target == _grid || e.target is ScrollView || e.target is Scroller || 
                    (e.target as VisualElement).ClassListContains("accessory-list-item") == false)
                {
                    var hero = _selector.Selected;

                    if (hero != null)
                    {
                        // 인벤토리 자체의 빈 공간을 자동으로 탐색하여 안전하게 안착
                        _itemService.UnequipToInventory(hero, _transferService.DraggingEquipmentSlotIndex, _totalInventory);
                    }
                    _transferService.ResetDrag();
                    e.StopPropagation(); // 가방 여백 영역에서 처리 끝났으므로 전파 중단
                }
            }
        });
    
        _totalInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    private void SyncSlots(int requiredCount)
    {
        while (_uiSlots.Count < requiredCount)
        {
            int uiIdx = _uiSlots.Count;
            var slot = new VisualElement();
            slot.AddToClassList("accessory-list-item"); // USS 적용

            slot.RegisterCallback<PointerDownEvent>(e => {
                if (uiIdx >= _mappedIndices.Count) return;
                int actualInvIdx = _mappedIndices[uiIdx];
                if (_totalInventory.GetSlot(actualInvIdx).IsEmpty) return;

                if (e.button == 0)
                {
                    _transferService.StartDrag(_totalInventory, actualInvIdx);
                }
                else if (e.button == 1)
                {
                    // 우클릭 자동 장착 수행 후 이벤트 전파를 완벽히 격리 차단 (이중 교체 트리거 방어)
                    _itemService.AutoEquip(_totalInventory, actualInvIdx);
                    e.StopPropagation();
                }
            });

            slot.RegisterCallback<PointerUpEvent>(e => {
                int targetBagSlotIdx = _mappedIndices[uiIdx];

                // 만약 장비 슬롯에서 드래그가 시작된 상태라면?
                if (_transferService.IsEquipmentDragging)
                {
                    var hero = _selector.Selected;
                    if (hero != null)
                    {
                        // 가방 정보를 직접 넘겨주며 직접 해제 연산 수행!
                        _itemService.UnequipToInventorySlot(
                            hero, 
                            _transferService.DraggingEquipmentSlotIndex, 
                            _totalInventory, 
                            targetBagSlotIdx
                        );
                    }
                    _transferService.ResetDrag();
                    return;
                }
                
                if (_transferService.IsDragging && uiIdx < _mappedIndices.Count) {
                    _transferService.ExecuteDragDrop(_transferService.CurrentSourceInventory, _transferService.CurrentDraggingIndex, _totalInventory, _mappedIndices[uiIdx]);
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
        _grid.style.flexDirection = FlexDirection.Column;
        SyncSlots(_mappedIndices.Count);

        for (int i = 0; i < _uiSlots.Count; i++)
        {
            var visual = _uiSlots[i]; visual.Clear();
            if (i >= _mappedIndices.Count) { visual.style.display = DisplayStyle.None; continue; }

            visual.style.display = DisplayStyle.Flex;
            var data = _totalInventory.GetSlot(_mappedIndices[i]);

            // 아이콘 (깨짐방지 클래스 적용)
            var icon = new VisualElement { style = { backgroundImage = new StyleBackground(data.item.icon) } };
            icon.AddToClassList("item-icon-pixelated");
            icon.pickingMode = PickingMode.Ignore;
            visual.Add(icon);

            // 이름
            visual.Add(new Label(data.item.DisplayName) { style = { color = Color.white, marginLeft = 10 }, pickingMode = PickingMode.Ignore });

            // 툴팁 이벤트
            visual.RegisterCallback<MouseEnterEvent>(evt => ShowTooltip(data, evt.mousePosition));
            visual.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
            visual.RegisterCallback<PointerDownEvent>(evt => HideTooltip());
        }
    }

    private void ShowTooltip(InventorySlot slot, Vector2 position)
    {
        if (slot.IsEmpty) return;
        _tooltip.Clear();
        
        var title = new Label(slot.item.DisplayName); title.AddToClassList("tooltip-title");
        _tooltip.Add(title);

        AddTooltipLabel(string.IsNullOrEmpty(slot.item.Description) ? "효과 없음" : slot.item.Description);

        if (slot.item is AccessoryItem acc) {
            AddTooltipLabel("\n[장착 효과]");
            foreach (var effect in acc.specialEffects)
            {
                var valueMode = effect.mode == ModifierMode.Flat ? $"{effect.value} 증가" : $"{effect.value:P1} 증가";
                AddTooltipLabel($"- {effect.name}: {effect.type} + {valueMode}");
            }
        }

        _tooltip.style.left = position.x + 20; _tooltip.style.top = position.y + 20;
        _tooltip.style.visibility = Visibility.Visible; _tooltip.BringToFront();
    }

    private void AddTooltipLabel(string text) { var l = new Label(text); l.AddToClassList("tooltip-text"); _tooltip.Add(l); }
    private void HideTooltip() => _tooltip.style.visibility = Visibility.Hidden;
    public void ToggleWindow() { var r = GetComponent<UIDocument>().rootVisualElement; r.style.display = (r.style.display == DisplayStyle.None) ? DisplayStyle.Flex : DisplayStyle.None; }
}