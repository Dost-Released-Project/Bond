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
                    if (e.button == 0) { 
                        InventoryView.CurrentDraggingIndex = _mappedIndices[uiIdx];
                        InventoryView.CurrentSourceInventory = _totalInventory;
                    } 
                    else if (e.button == 1) { 
                        // 1. 장착 실행
                        bool success = _equipService.AutoEquip(_totalInventory, _mappedIndices[uiIdx]);
            
                        // 2. [핵심] 성공여부와 상관없이 즉시 UI 갱신 (도돌이표 방지)
                        RefreshUI(); 
                    }
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
            // [추가] 슬롯 자체가 이벤트를 잘 받도록 설정
            _uiSlots[i].pickingMode = PickingMode.Position; 

            if (i < _mappedIndices.Count) {
                var data = _totalInventory.GetSlot(_mappedIndices[i]);
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(data.item.icon);
            
                // [중요] 아이콘이 마우스 클릭을 막지 않게 설정 (슬롯이 클릭을 받아야 하므로)
                icon.pickingMode = PickingMode.Ignore; 
            
                icon.style.width = icon.style.height = Length.Percent(100);
                _uiSlots[i].Add(icon);
            }
        }
    }
}