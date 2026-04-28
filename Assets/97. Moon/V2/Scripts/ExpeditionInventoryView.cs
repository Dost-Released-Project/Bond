using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class ExpeditionInventoryView : MonoBehaviour
{
    private IExpeditionInventory _expeditionInventory;
    private VisualElement _root;
    private List<VisualElement> _slots = new();

    [Inject]
    public void Construct(IExpeditionInventory inventory)
    {
        _expeditionInventory = inventory;
        // 데이터 변경 시 UI 갱신 이벤트 구독
        _expeditionInventory.OnChanged += RefreshUI;
    }

    private void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("expedition-container");
        SetupSlots();
        RefreshUI(); // 초기화 시 한 번 실행
    }

    private void SetupSlots()
    {
        _slots.Clear();
        _root.Clear();

        for (int i = 0; i < _expeditionInventory.Capacity; i++)
        {
            int index = i;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");
            
            // 우클릭: 사용
            slot.RegisterCallback<PointerDownEvent>(evt => {
                if (evt.button == 1) UseItemInExpedition(index);
            });

            // 드래그 앤 드롭 처리는 InventoryView의 공통 로직을 사용하거나 
            // 여기에 PointerDown(드래그 시작), PointerUp(드랍)을 추가해야 합니다.

            _root.Add(slot);
            _slots.Add(slot);
        }
    }

    private void UseItemInExpedition(int index)
    {
        var slot = _expeditionInventory.GetSlot(index);
        if (slot.IsEmpty || slot.item.category != ItemCategory.Consume) return;

        if (AdminTestTool.testHero != null)
        {
            slot.item.Use(AdminTestTool.testHero);
            _expeditionInventory.RemoveFromSlot(index, 1);
        }
    }
    
    public void RefreshUI() 
    {
        for (int i = 0; i < _slots.Count; i++)
        {
            var slotData = _expeditionInventory.GetSlot(i);
            var visual = _slots[i];
            visual.Clear();

            if (!slotData.IsEmpty)
            {
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(slotData.item.icon);
                icon.style.width = Length.Percent(100);
                icon.style.height = Length.Percent(100);
                visual.Add(icon);
            }
        }
    }

    private void OnDestroy()
    {
        if (_expeditionInventory != null)
            _expeditionInventory.OnChanged -= RefreshUI;
    }
}