using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using System.Collections.Generic;

public class EquipmentSlotUI : MonoBehaviour
{
    private VisualElement _root;
    // 단일 슬롯이 아닌 슬롯 리스트를 관리합니다.
    private List<VisualElement> _accSlots = new();
    private CharacterEquipService _equipService;

    [Inject]
    public void Construct(CharacterEquipService es) => _equipService = es;

    private void Start()
    {
        var uiDoc = GetComponent<UIDocument>();
        if (uiDoc == null || uiDoc.rootVisualElement == null) {
            Debug.LogError($"{gameObject.name}: UIDocument 또는 Root가 없습니다.");
            return;
        }

        _root = uiDoc.rootVisualElement;

        // [핵심] 영웅의 장비 슬롯 개수만큼 루프를 돌며 슬롯을 찾아 등록합니다.
        var hero = AdminTestTool.testHero;
        if (hero != null && hero.Data != null)
        {
            for (int i = 0; i < hero.Data.Equips.Length; i++)
            {
                int index = i; // 클로저 캡처 방지
                var slotVisual = _root.Q<VisualElement>($"char-acc-slot-{index}");
                
                if (slotVisual != null)
                {
                    _accSlots.Add(slotVisual);
                    RegisterEvents(slotVisual, index);
                }
            }
        }

        // 서비스 이벤트를 구독하여 데이터 변경 시 전체 슬롯 갱신
        _equipService.OnEquipmentChanged += RefreshUI;
        RefreshUI();
    }

    private void RegisterEvents(VisualElement slotVisual, int index)
    {
        // 드래그 드롭 (장착)
        slotVisual.RegisterCallback<PointerUpEvent>(evt => {
            if (InventoryView.CurrentSourceInventory != null) {
                _equipService.EquipFromDrag(InventoryView.CurrentSourceInventory, InventoryView.CurrentDraggingIndex, index);
                InventoryView.ResetDraggingState();
            }
        });

        // 우클릭 (해제)
        slotVisual.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.button == 1) _equipService.UnequipToInventory(AdminTestTool.testHero, index);
        });
    }

    public void ToggleWindow() => _root.style.display = (_root.style.display == DisplayStyle.Flex) ? DisplayStyle.None : DisplayStyle.Flex;

    public void RefreshUI()
    {
        var hero = AdminTestTool.testHero;
        if (hero?.Data?.Equips == null) return;

        // 리스트에 등록된 모든 슬롯을 순회하며 데이터 동기화
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
        if (_equipService != null)
            _equipService.OnEquipmentChanged -= RefreshUI;
    }
}