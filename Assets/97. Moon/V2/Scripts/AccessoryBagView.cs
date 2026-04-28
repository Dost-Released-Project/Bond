using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class AccessoryBagView : MonoBehaviour
{
    private ITotalInventory _totalInventory;
    private CharacterEquipService _equipService;
    private List<VisualElement> _uiSlots = new();
    private List<int> _mappedIndices = new();

    [Inject]
    public void Construct(ITotalInventory total, CharacterEquipService equip) { _totalInventory = total; _equipService = equip; }

    private void Start()
    {
        var grid = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("accessory-grid");
        for (int i = 0; i < 12; i++) {
            int uiIdx = i;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");
            slot.RegisterCallback<PointerDownEvent>(e => {
                if (uiIdx < _mappedIndices.Count) {
                    if (e.button == 0) { // 좌클릭 드래그용
                        InventoryView.CurrentDraggingIndex = _mappedIndices[uiIdx];
                        InventoryView.CurrentSourceInventory = _totalInventory;
                    } else if (e.button == 1) _equipService.AutoEquip(_totalInventory, _mappedIndices[uiIdx]);
                }
            });
            grid.Add(slot);
            _uiSlots.Add(slot);
        }
        _totalInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    public void RefreshUI()
    {
        _mappedIndices = _totalInventory.GetFilteredIndices("", ItemCategory.Accessories).ToList();
        for (int i = 0; i < _uiSlots.Count; i++) {
            _uiSlots[i].Clear();
            if (i < _mappedIndices.Count) {
                var data = _totalInventory.GetSlot(_mappedIndices[i]);
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(data.item.icon);
                icon.style.width = icon.style.height = Length.Percent(100);
                _uiSlots[i].Add(icon);
            }
        }
    }
}