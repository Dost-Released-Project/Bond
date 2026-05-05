using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using System.Collections.Generic;

public class EquipmentSlotUI : MonoBehaviour
{
    private VisualElement _root;
    private List<VisualElement> _accSlots = new();
    
    private CharacterItemService _itemService;
    private InventoryTransferService _transferService; // 드래그 상태 확인을 위해 추가
    private IInventory _currentContextInventory; 

    [Inject]
    public void Construct(CharacterItemService itemService, InventoryTransferService transferService, IInventory contextInventory)
    {
        _itemService = itemService;
        _transferService = transferService;
        _currentContextInventory = contextInventory; 
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
        // [드래그 장착] 마우스를 뗐을 때 트랜스퍼 서비스에 저장된 데이터로 장착 실행
        slotVisual.RegisterCallback<PointerUpEvent>(evt => {
            if (_transferService.IsDragging) 
            {
                _itemService.EquipFromDrag(_transferService.CurrentSourceInventory, _transferService.CurrentDraggingIndex, index);
                _transferService.ResetDrag();
            }
        });

        slotVisual.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.button == 1) 
                _itemService.UnequipToInventory(AdminTestTool.testHero, index, _currentContextInventory);
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