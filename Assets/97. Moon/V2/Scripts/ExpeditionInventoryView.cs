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
    }

    private void Start()
    {
        _root = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>("expedition-container");
        
        _expeditionInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    // 용량(Capacity)에 맞춰 슬롯 UI 객체를 동기화
    private void SyncSlots()
    {
        while (_slots.Count < _expeditionInventory.Capacity)
        {
            int index = _slots.Count;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");
            
            // 우클릭: 사용 로직
            slot.RegisterCallback<PointerDownEvent>(evt => {
                if (evt.button == 1) UseItemInExpedition(index);
                // 필요 시 좌클릭 드래그 시작 로직을 여기에 추가 (InventoryView 참고)
            });

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
        // 1. [핵심] 건설 등으로 용량이 늘어났을 경우를 대비해 슬롯 UI 생성
        SyncSlots();

        // 2. 기존 슬롯 데이터 갱신
        for (int i = 0; i < _slots.Count; i++)
        {
            var slotData = _expeditionInventory.GetSlot(i);
            var visual = _slots[i];
            visual.Clear();

            if (!slotData.IsEmpty)
            {
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(slotData.item.icon);
                icon.style.width = icon.style.height = Length.Percent(100);
                icon.pickingMode = PickingMode.Ignore;
                visual.Add(icon);

                // 수량 표시가 필요하다면 여기에 Label 추가 로직 삽입
            }
        }
    }

    private void OnDestroy()
    {
        if (_expeditionInventory != null)
            _expeditionInventory.OnChanged -= RefreshUI;
    }
}