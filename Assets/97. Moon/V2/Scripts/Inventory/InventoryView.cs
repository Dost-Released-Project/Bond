using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using VContainer;

public class InventoryView : MonoBehaviour
{
    private VisualElement _root, _totalGrid, _expeditionGrid, _dragGhost;
    private List<VisualElement> _totalSlotElements = new(), _expeditionSlotElements = new();
    private TextField _searchField;
    private ScrollView _totalScroll;

    private ITotalInventory _totalInventory;
    private IExpeditionInventory _expeditionInventory;
    private InventoryTransferService _transferService;
    private CharacterItemService _itemService; // 변경된 서비스
    private InventoryUIService _uiService;     // 추가된 서비스
    
    private string _currentSearch = "";
    private ItemCategory? _currentFilter = null;

    [Inject]
    public void Construct(ITotalInventory total, IExpeditionInventory expedition, 
        InventoryTransferService service, CharacterItemService itemService, InventoryUIService uiService)
    {
        _totalInventory = total; _expeditionInventory = expedition;
        _transferService = service; _itemService = itemService; _uiService = uiService;
    }

    private void Start()
    {
        SetupUI();
        
        // 생성된 ID를 기반으로 아이템 로드하여 초기 세팅 (예시 ID 사용) 테스트 용도
        BaseItem item_oldBandage = Resources.Load<BaseItem>("Data/Items/Consumables/07000000");
        BaseItem item_oldSedative = Resources.Load<BaseItem>("Data/Items/Consumables/07010000");
        BaseItem item_stimulant = Resources.Load<BaseItem>("Data/Items/Consumables/07020000");
        BaseItem item_bandage = Resources.Load<BaseItem>("Data/Items/Consumables/07030000");
        BaseItem item_sedative = Resources.Load<BaseItem>("Data/Items/Consumables/07040000");
        BaseItem item_ring1 = Resources.Load<BaseItem>("Data/Items/Accessories/08000000");
        BaseItem item_ring2 = Resources.Load<BaseItem>("Data/Items/Accessories/08010000");
        BaseItem item_ring3 = Resources.Load<BaseItem>("Data/Items/Accessories/08020000");
        BaseItem item_ring4 = Resources.Load<BaseItem>("Data/Items/Accessories/08030000");

        if (item_oldBandage != null) _totalInventory.AddItemAt(0, item_oldBandage, 5);
        if (item_bandage != null) _totalInventory.AddItemAt(1, item_bandage, 5);
        if (item_stimulant != null) _totalInventory.AddItemAt(2, item_stimulant, 5);
        if (item_oldSedative != null) _totalInventory.AddItemAt(3, item_oldSedative, 5);
        if (item_sedative != null) _totalInventory.AddItemAt(4, item_sedative, 5);
        if (item_ring1 != null) _totalInventory.AddItemAt(5, item_ring1, 1);
        if (item_ring2 != null) _totalInventory.AddItemAt(6, item_ring2, 1);
        if (item_ring3 != null) _totalInventory.AddItemAt(7, item_ring3, 1);
        if (item_ring4 != null) _totalInventory.AddItemAt(8, item_ring4, 1);
        
        ToggleWindow(false);
    }

    private void SetupUI()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _totalScroll = _root.Q<ScrollView>("total-inventory-grid");
        _totalGrid = _root.Q<VisualElement>("total-grid");
        _expeditionGrid = _root.Q<VisualElement>("expedition-inventory-grid");

        // 드래그 고스트 설정 (UIService로 관리)
        _dragGhost = new VisualElement();
        _dragGhost.style.position = Position.Absolute;
        _dragGhost.pickingMode = PickingMode.Ignore;
        _dragGhost.style.visibility = Visibility.Hidden;
        _dragGhost.style.width = 60; _dragGhost.style.height = 60;
        _root.Add(_dragGhost);
        
        _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);

        // 버튼 및 필드 연결 (기존 기능 100% 유지)
        _searchField = _root.Q<TextField>("inventory-search");
        _searchField?.RegisterValueChangedCallback(evt => { _currentSearch = evt.newValue; RefreshUI(); });

        _root.Q<Button>("btn-sort")?.RegisterCallback<ClickEvent>(evt => { 
            _totalInventory.SortById(); _expeditionInventory.SortById(); RefreshUI(); 
        });
        _root.Q<Button>("btn-filter-consumable")?.RegisterCallback<ClickEvent>(evt => SetFilter(ItemCategory.Consume));
        _root.Q<Button>("btn-filter-accessory")?.RegisterCallback<ClickEvent>(evt => SetFilter(ItemCategory.Accessories));
        _root.Q<Button>("btn-filter-all")?.RegisterCallback<ClickEvent>(evt => SetFilter(null));
        _root.Q<Button>("btn-close")?.RegisterCallback<ClickEvent>(evt => ToggleWindow(false));

        _totalInventory.OnChanged += RefreshUI;
        _expeditionInventory.OnChanged += RefreshUI;
        
        SyncSlotCount(_totalGrid, _totalInventory.Capacity, _totalSlotElements, _totalInventory);
        SyncSlotCount(_expeditionGrid, _expeditionInventory.Capacity, _expeditionSlotElements, _expeditionInventory);
    }

    public void ToggleWindow(bool show)
    {
        _uiService.IsInventoryWindowActive = show; // UIService 상태 갱신
        _root.Q<VisualElement>("inventory-container").style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (show) { if (_totalScroll != null) _totalScroll.scrollOffset = Vector2.zero; RefreshUI();}
    }

    private void SyncSlotCount(VisualElement container, int targetCount, List<VisualElement> list, IInventory inv)
    {
        while (list.Count < targetCount)
        {
            int index = list.Count;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");
            slot.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt, inv, index));
            slot.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt, inv, index));
            container.Add(slot);
            list.Add(slot);
        }
    }

    public void RefreshUI()
    {
        SyncSlotCount(_totalGrid, _totalInventory.Capacity, _totalSlotElements, _totalInventory);
        SyncSlotCount(_expeditionGrid, _expeditionInventory.Capacity, _expeditionSlotElements, _expeditionInventory);

        var totalVisible = _totalInventory.GetFilteredIndices(_currentSearch, _currentFilter);
        var expeditionVisible = _expeditionInventory.GetFilteredIndices(_currentSearch, _currentFilter);

        UpdateGrid(_totalSlotElements, _totalInventory, totalVisible);
        UpdateGrid(_expeditionSlotElements, _expeditionInventory, expeditionVisible);
    }

    private void UpdateGrid(List<VisualElement> elements, IInventory inv, IEnumerable<int> visibleIndices)
    {
        var visibleSet = new HashSet<int>(visibleIndices);
        for (int i = 0; i < elements.Count; i++)
        {
            var visual = elements[i];
            visual.Clear();
            if (visibleSet.Contains(i))
            {
                visual.style.display = DisplayStyle.Flex;
                var data = inv.GetSlot(i);
                if (!data.IsEmpty)
                {
                    var icon = new VisualElement();
                    icon.style.backgroundImage = new StyleBackground(data.item.icon);
                    icon.style.width = Length.Percent(100); icon.style.height = Length.Percent(100);
                    visual.Add(icon);
                    var label = new Label(data.quantity.ToString());
                    label.AddToClassList("slot-quantity-label");
                    visual.Add(label);
                }
            }
            else visual.style.display = DisplayStyle.None;
        }
    }

    private void OnPointerDown(PointerDownEvent evt, IInventory inv, int index)
    {
        var slot = inv.GetSlot(index);
        if (evt.button == 0)
        {
            if (slot.IsEmpty) return;
            // UIService를 통해 드래그 시작
            _uiService.StartDrag(inv, index, slot.item.icon, _dragGhost, evt.position, new Vector2(30, 30));
        }
        else if (evt.button == 1)
        {
            var target = (inv is ITotalInventory) ? (IInventory)_expeditionInventory : (IInventory)_totalInventory;
            _transferService.MoveOneFromSlot(inv, index, target);
        }
    }

    private void OnPointerUp(PointerUpEvent evt, IInventory targetInv, int targetIndex)
    {
        if (_uiService.CurrentSourceInventory != null)
        {
            _transferService.ExecuteDragDrop(_uiService.CurrentSourceInventory, _uiService.CurrentDraggingIndex, targetInv, targetIndex);
            _uiService.ResetDrag();
        }
    }

    private void OnPointerMove(PointerMoveEvent evt) { 
        if (_uiService.CurrentSourceInventory != null) _uiService.UpdateGhostPosition(evt.position, new Vector2(30, 30)); 
    }

    private void SetFilter(ItemCategory? cat) { _currentFilter = cat; RefreshUI(); }
}