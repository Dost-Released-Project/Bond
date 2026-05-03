using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using System.Collections.Generic;

public class EquipmentSlotUI : MonoBehaviour
{
    private VisualElement _root;
    private List<VisualElement> _accSlots = new();
    
    private CharacterItemService _itemService; // 이름 및 타입 변경
    private InventoryUIService _uiService;     // 드래그 상태 확인용 추가

    [Inject]
    public void Construct(CharacterItemService itemService, InventoryUIService uiService)
    {
        _itemService = itemService;
        _uiService = uiService;
    }

    private void Start()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null || uiDoc.rootVisualElement == null) return;

        _root = uiDoc.rootVisualElement;

        var hero = AdminTestTool.testHero;
        if (hero != null && hero.Data != null)
        {
            for (int i = 0; i < hero.Data.Equips.Length; i++)
            {
                int index = i; 
                var slotVisual = _root.Q<VisualElement>($"char-acc-slot-{index}");
                
                if (slotVisual != null)
                {
                    _accSlots.Add(slotVisual);
                    RegisterEvents(slotVisual, index);
                }
            }
        }

        _itemService.OnEquipmentChanged += RefreshUI;
        RefreshUI();
    }

    private void RegisterEvents(VisualElement slotVisual, int index)
    {
        // [장착] 드래그 드롭 성공 시
        slotVisual.RegisterCallback<PointerUpEvent>(evt => {
            if (_uiService.CurrentSourceInventory != null) {
                // UIService를 통해 소스 인벤토리와 인덱스 참조
                _itemService.EquipFromDrag(_uiService.CurrentSourceInventory, _uiService.CurrentDraggingIndex, index);
                _uiService.ResetDrag();
            }
        });

        // [해제] 우클릭 시 인벤토리로 반환
        slotVisual.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.button == 1) _itemService.UnequipToInventory(AdminTestTool.testHero, index);
        });
    }

    public void ToggleWindow()
    {
        bool isShowing = _root.style.display == DisplayStyle.Flex;
        _root.style.display = isShowing ? DisplayStyle.None : DisplayStyle.Flex;
    }

    public void RefreshUI()
    {
        var hero = AdminTestTool.testHero;
        if (hero?.Data?.Equips == null) return;

        for (int i = 0; i < _accSlots.Count; i++)
        {
            var visual = _accSlots[i];
            visual.Clear();

            if (i < hero.Data.Equips.Length)
            {
                var eq = hero.Data.Equips[i];
                if (eq?.originItem != null)
                {
                    var icon = new VisualElement();
                    icon.style.backgroundImage = new StyleBackground(eq.originItem.icon);
                    icon.style.width = icon.style.height = Length.Percent(100);
                    icon.pickingMode = PickingMode.Ignore;
                    visual.Add(icon);
                }
            }
        }
    }

    private void OnDestroy()
    {
        if (_itemService != null) _itemService.OnEquipmentChanged -= RefreshUI;
    }
}