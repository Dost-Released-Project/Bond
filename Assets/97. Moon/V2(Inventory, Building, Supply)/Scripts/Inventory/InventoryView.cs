using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using VContainer;

[RequireComponent(typeof(UIDocument))]
public class InventoryView : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _totalGrid;
    private VisualElement _expeditionGrid;
    private VisualElement _dragGhost; // 드래그 시 보여줄 아이콘

    private List<VisualElement> _totalSlotElements = new();
    private List<VisualElement> _expeditionSlotElements = new();

    private ITotalInventory _totalInventory;
    private IExpeditionInventory _expeditionInventory;
    private InventoryTransferService _transferService;

    public BaseItem[] testItems;

    // 드래그 상태 관리
    private int _sourceIndex = -1;
    private IInventory _sourceInventory;
    
    private TextField _searchField;
    private Button _sortButton;
    private Button _filterConsumableBtn;
    private Button _filterAccessoryBtn;
    private Button _allFilterBtn;
    
    private string _currentSearch = "";
    private ItemCategory? _currentFilter = null; // null이면 전체 보기
    
    // [Inject] 태그를 붙이면 VContainer가 알아서 값을 넣어줍니다.
    [Inject]
    public void Construct(ITotalInventory total, IExpeditionInventory expedition, InventoryTransferService service)
    {
        _totalInventory = total;
        _expeditionInventory = expedition;
        _transferService = service;
    }

    // Awake 대신 Start를 사용하여 UI Toolkit의 로드 시간을 벌어줍니다.
    private void Start()
    {
        var uiDocument = GetComponent<UIDocument>();
        _root = uiDocument.rootVisualElement;

        // 여기서 한 번 더 체크 (UI가 로드되지 않았다면 다음 프레임에 실행되게 할 수도 있음)
        if (_root == null || _root.childCount == 0) 
        {
            Debug.LogWarning("UI Root가 아직 준비되지 않았습니다. 잠시 후 다시 시도합니다.");
            Invoke(nameof(SetupUI), 0.1f); // 0.1초 뒤 실행
            return;
        }
        
        _totalInventory.AddItemAt(0, testItems[0], 5); 
        _totalInventory.AddItemAt(1, testItems[1], 5); 
        _totalInventory.AddItemAt(2, testItems[2], 5);
        _totalInventory.AddItemAt(3, testItems[3], 1);
        _totalInventory.AddItemAt(4, testItems[4], 1);
        RefreshUI();

        SetupUI();
    }

    private void SetupUI()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        
        // 캡처해주신 이미지의 이름과 정확히 일치해야 합니다 (#은 ID, .은 Class)
        _totalGrid = _root.Q<VisualElement>("total-inventory-grid");
        _expeditionGrid = _root.Q<VisualElement>("expedition-inventory-grid");

        if (_totalGrid == null || _expeditionGrid == null)
        {
            Debug.LogError("UXML에서 grid를 찾을 수 없습니다. Name 설정을 확인하세요.");
            return;
        }

        // 고스트 생성 및 이벤트 등록 (기존 로직 동일)
        _dragGhost = new VisualElement();
        _dragGhost.style.position = Position.Absolute;
        _dragGhost.pickingMode = PickingMode.Ignore;
        _dragGhost.style.visibility = Visibility.Hidden;
        _root.Add(_dragGhost);
        _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);

        GenerateSlots(_totalGrid, _totalInventory.Capacity, _totalSlotElements, _totalInventory);
        GenerateSlots(_expeditionGrid, _expeditionInventory.Capacity, _expeditionSlotElements, _expeditionInventory);
        
        RefreshUI();
        
        // UI 요소 연결 (UXML에 해당 이름의 요소가 있어야 함)
        _searchField = _root.Q<TextField>("inventory-search");
        _sortButton = _root.Q<Button>("btn-sort");
        _filterConsumableBtn = _root.Q<Button>("btn-filter-consumable");
        _filterAccessoryBtn = _root.Q<Button>("btn-filter-accessory");
        _allFilterBtn = _root.Q<Button>("btn-filter-all");
        
        // 검색 기능 연결
        _searchField.RegisterValueChangedCallback(evt => {
            _currentSearch = evt.newValue;
            RefreshUI();
        });
        
        // 이벤트 등록
        _sortButton?.RegisterCallback<ClickEvent>(evt => {
            _totalInventory.SortById();
            _expeditionInventory.SortById();
            RefreshUI();
        });

        _filterConsumableBtn.RegisterCallback<ClickEvent>(evt => SetFilter(ItemCategory.Consume));
        _filterAccessoryBtn.RegisterCallback<ClickEvent>(evt => SetFilter(ItemCategory.Accessories));
        _allFilterBtn.RegisterCallback<ClickEvent>(evt => SetFilter(null)); // 전체 보기 버튼도 필요함
    }
    
    private void SetFilter(ItemCategory? category)
    {
        _currentFilter = category;
        RefreshUI();
        Debug.Log(category == null ? "전체 보기" : $"{category} 필터 활성화");
    }

    private void GenerateSlots(VisualElement container, int count, List<VisualElement> list, IInventory inv)
    {
        container.Clear();
        list.Clear();

        for (int i = 0; i < count; i++)
        {
            int index = i;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");
            
            slot.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt, inv, index));
            slot.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt, inv, index));

            container.Add(slot);
            list.Add(slot);
        }
    }

    private void RefreshUI()
    {
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
            var slotVisual = elements[i];
            var slotData = inv.GetSlot(i);
            slotVisual.Clear();

            if (visibleSet.Contains(i))
            {
                slotVisual.style.display = DisplayStyle.Flex;
            
                if (!slotData.IsEmpty)
                {
                    // 아이콘 생성
                    var icon = new VisualElement();
                    icon.style.backgroundImage = new StyleBackground(slotData.item.icon);
                    icon.style.width = Length.Percent(100);
                    icon.style.height = Length.Percent(100);
                
                    // 수량 라벨
                    var label = new Label(slotData.quantity.ToString());
                    label.AddToClassList("slot-quantity-label");
                
                    slotVisual.Add(icon);
                    slotVisual.Add(label);
                }
            }
            else
            {
                slotVisual.style.display = DisplayStyle.None;
            }
        }
    }

    private void OnPointerDown(PointerDownEvent evt, IInventory inv, int index)
    {
        if (evt.button == 0) // 좌클릭 드래그 시작
        {
            var slot = inv.GetSlot(index);
            if (slot.IsEmpty) return;
            _sourceIndex = index;
            _sourceInventory = inv;
            
            _dragGhost.style.backgroundImage = new StyleBackground(slot.item.icon);
            _dragGhost.style.width = 60; // 적절한 크기
            _dragGhost.style.height = 60;
            _dragGhost.style.visibility = Visibility.Visible;
            UpdateGhostPosition(evt.position);
        }
        else if (evt.button == 1) // 우클릭: 1개씩 이동
        {
            var target = (inv is ITotalInventory) ? (IInventory)_expeditionInventory : (IInventory)_totalInventory;
            _transferService.MoveOneFromSlot(inv, index, target); // 수정된 메서드 호출
            RefreshUI();
        }
    }

    private void OnPointerUp(PointerUpEvent evt, IInventory targetInv, int targetIndex)
    {
        if (_sourceInventory != null)
        {
            // 드래그 앤 드롭 시 전체 스택 이동 시도 (서비스에서 제한량 체크함)
            _transferService.ExecuteDragDrop(_sourceInventory, _sourceIndex, targetInv, targetIndex);
            
            _sourceInventory = null;
            _sourceIndex = -1;
            _dragGhost.style.visibility = Visibility.Hidden;
            RefreshUI();
        }
    }    

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (_sourceInventory != null)
        {
            UpdateGhostPosition(evt.position);
        }
    }

    private void UpdateGhostPosition(Vector2 position)
    {
        // 씬 중앙이나 레이아웃 설정에 따라 좌표 보정이 필요할 수 있습니다.
        _dragGhost.style.left = position.x - (_dragGhost.layout.width / 2);
        _dragGhost.style.top = position.y - (_dragGhost.layout.height / 2);
    }
}