using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using System.Collections.Generic;

public class EquipmentSlotUI : MonoBehaviour
{
    private VisualElement _root, _tooltip;
    private List<VisualElement> _accSlots = new();
    private CharacterItemService _itemService;
    private InventoryTransferService _transferService;
    
    [Inject] private CharacterSelector _selector;

    [Inject]
    public void Construct(CharacterItemService itemService, InventoryTransferService transferService)
    {
        _itemService = itemService; _transferService = transferService;
    }

    private void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        
        // 툴팁 생성
        _tooltip = new VisualElement();
        _tooltip.AddToClassList("inventory-tooltip");
        _tooltip.pickingMode = PickingMode.Ignore;
        _root.Add(_tooltip);

        // [수정] 캐릭터 선택 여부와 관계없이 UI에 고정 배치된 슬롯 2개를 미리 캐싱하고 이벤트를 연결합니다.
        for (int i = 0; i < 2; i++)
        {
            var slotVisual = _root.Q<VisualElement>($"char-acc-slot-{i}");
            if (slotVisual != null) { _accSlots.Add(slotVisual); RegisterEvents(slotVisual, i); }
        }

        _itemService.OnEquipmentChanged += RefreshUI;
        _selector.OnSelectionChanged += HandleSelectionChanged; // [추가] 캐릭터 선택 변경 이벤트 구독
        
        RefreshUI();
    }

    // [추가] 캐릭터가 변경되었을 때 실행될 콜백 함수
    private void HandleSelectionChanged(BaseCharacter newHero)
    {
        RefreshUI();
    }

    private void RegisterEvents(VisualElement slotVisual, int index)
    {
        slotVisual.RegisterCallback<PointerDownEvent>(evt => {
            HideTooltip();
            if (evt.button == 0) {
                var acc = _selector.Selected?.Data?.Accessories?[index]; // [안전성] ?. 추가
                if (acc != null) _transferService.StartEquipmentDrag(index);
            }
        });

        slotVisual.RegisterCallback<PointerUpEvent>(evt => {
            if (_transferService.IsDragging) {
                _itemService.EquipFromDrag(_transferService.CurrentSourceInventory, _transferService.CurrentDraggingIndex, index);
                _transferService.ResetDrag();
            }
        });

        // 장비 슬롯 툴팁 이벤트 추가
        slotVisual.RegisterCallback<MouseEnterEvent>(evt => {
            var acc = _selector.Selected?.Data?.Accessories?[index]; // [안전성] ?. 추가
            if (acc != null) ShowTooltip(new InventorySlot { item = acc, quantity = 1 }, evt.mousePosition);
        });
        slotVisual.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
    }

    public void RefreshUI()
    {
        var hero = _selector.Selected;

        // [개선] 슬롯 비우기(Clear)는 캐릭터가 null일 때도 정상 작동하여 UI를 비워주어야 하므로 루프 구조를 정돈했습니다.
        for (int i = 0; i < _accSlots.Count; i++)
        {
            var visual = _accSlots[i]; visual.Clear();
            if (hero?.Data?.Accessories == null || i >= hero.Data.Accessories.Length) continue;

            var acc = hero.Data.Accessories[i];
            if (acc != null)
            {
                var icon = new VisualElement { style = { backgroundImage = new StyleBackground(acc.icon) } };
                icon.AddToClassList("item-icon-pixelated"); // 깨짐 방지 클래스
                icon.style.width = Length.Percent(100); icon.style.height = Length.Percent(100);
                icon.pickingMode = PickingMode.Ignore;
                visual.Add(icon);
            }
        }
    }
    
    public void ToggleWindow()
    {
        var root = GetComponent<UIDocument>().rootVisualElement;
        root.style.display = (root.style.display == DisplayStyle.None) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    // 툴팁 로직 (중복 최소화 위해 내부 구현)
    private void ShowTooltip(InventorySlot slot, Vector2 position)
    {
        _tooltip.Clear();
        var title = new Label(slot.item.DisplayName); title.AddToClassList("tooltip-title");
        _tooltip.Add(title);
        _tooltip.Add(new Label(slot.item.Description) { style = { color = Color.white, whiteSpace = WhiteSpace.Normal } });
        
        if (slot.item is AccessoryItem acc) {
            AddTooltipLabel("\n[장착 효과]");
            foreach (var effect in acc.specialEffects)
            {
                var valueMode = effect.mode == ModifierMode.Flat ? $"{effect.value} 증가" : $"{effect.value:P1} 증가";
                AddTooltipLabel($"- {effect.name}: {effect.type} + {valueMode}");
            }
        }
        
        _tooltip.style.left = position.x + 20; _tooltip.style.top = position.y - 200;
        _tooltip.style.visibility = Visibility.Visible; _tooltip.BringToFront();
    }
    
    private void AddTooltipLabel(string text)
    {
        var label = new Label(text);
        label.AddToClassList("tooltip-text");
        _tooltip.Add(label);
    }
    
    private void HideTooltip() => _tooltip.style.visibility = Visibility.Hidden;
    
    private void OnDestroy() 
    { 
        if (_itemService != null) _itemService.OnEquipmentChanged -= RefreshUI; 
        if (_selector != null) _selector.OnSelectionChanged -= HandleSelectionChanged; // [추가] 메모리 누수 방지 구독 해제
    }
}