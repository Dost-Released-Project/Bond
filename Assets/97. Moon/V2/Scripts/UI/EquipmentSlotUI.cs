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

        var hero = AdminTestTool.testHero;
        //var hero = _selector.Selected;
        
        if (hero?.Data != null)
        {
            for (int i = 0; i < hero.Data.Equips.Length; i++)
            {
                var slotVisual = _root.Q<VisualElement>($"char-acc-slot-{i}");
                if (slotVisual != null) { _accSlots.Add(slotVisual); RegisterEvents(slotVisual, i); }
            }
        }

        _itemService.OnEquipmentChanged += RefreshUI;
        RefreshUI();
    }

    private void RegisterEvents(VisualElement slotVisual, int index)
    {
        slotVisual.RegisterCallback<PointerDownEvent>(evt => {
            HideTooltip();
            if (evt.button == 0) {
                var eq = AdminTestTool.testHero.Data.Equips[index];
                //var eq = _selector.Selected.Data.Equips[index];
                if (eq?.originItem != null) _transferService.StartEquipmentDrag(index);
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
            var eq = AdminTestTool.testHero.Data.Equips[index];
            //var eq = _selector.Selected.Data.Equips[index];
            if (eq?.originItem != null) ShowTooltip(new InventorySlot { item = eq.originItem, quantity = 1 }, evt.mousePosition);
        });
        slotVisual.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());
    }

    public void RefreshUI()
    {
        var hero = AdminTestTool.testHero;
        //var hero = _selector.Selected;
        if (hero?.Data?.Equips == null) return;

        for (int i = 0; i < _accSlots.Count; i++)
        {
            var visual = _accSlots[i]; visual.Clear();
            if (i >= hero.Data.Equips.Length) continue;

            var eq = hero.Data.Equips[i];
            if (eq?.originItem != null)
            {
                var icon = new VisualElement { style = { backgroundImage = new StyleBackground(eq.originItem.icon) } };
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
        
        _tooltip.style.left = position.x + 20; _tooltip.style.top = position.y + 20;
        _tooltip.style.visibility = Visibility.Visible; _tooltip.BringToFront();
    }
    private void HideTooltip() => _tooltip.style.visibility = Visibility.Hidden;
    private void OnDestroy() { if (_itemService != null) _itemService.OnEquipmentChanged -= RefreshUI; }
}