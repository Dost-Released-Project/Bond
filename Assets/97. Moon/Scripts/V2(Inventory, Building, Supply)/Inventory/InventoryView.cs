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
        
        _totalInventory.AddItem(testItems[0], 5); 
        _totalInventory.AddItem(testItems[1], 5); 
        _totalInventory.AddItem(testItems[2], 5); 
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

    public void RefreshUI()
    {
        UpdateGrid(_totalSlotElements, _totalInventory);
        UpdateGrid(_expeditionSlotElements, _expeditionInventory);
    }

    private void UpdateGrid(List<VisualElement> elements, IInventory inv)
    {
        for (int i = 0; i < elements.Count; i++)
        {
            var slotData = inv.GetSlot(i);
            var slotVisual = elements[i];
            slotVisual.Clear();

            if (!slotData.IsEmpty)
            {
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(slotData.item.icon);
                icon.style.width = Length.Percent(100);
                icon.style.height = Length.Percent(100);
                
                var label = new Label(slotData.quantity.ToString());
                label.AddToClassList("slot-quantity-label");
                
                slotVisual.Add(icon);
                slotVisual.Add(label);
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

            // 드래그 고스트 활성화
            _dragGhost.style.backgroundImage = new StyleBackground(slot.item.icon);
            _dragGhost.style.width = 60; // 적절한 크기
            _dragGhost.style.height = 60;
            _dragGhost.style.visibility = Visibility.Visible;
            UpdateGhostPosition(evt.position);
        }
        else if (evt.button == 1) // 우클릭 즉시 이동
        {
            var target = (inv is ITotalInventory) ? (IInventory)_expeditionInventory : (IInventory)_totalInventory;
            _transferService.TryMoveItem(inv, index, target);
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

    private void OnPointerUp(PointerUpEvent evt, IInventory targetInv, int targetIndex)
    {
        if (_sourceInventory != null)
        {
            _transferService.SwapItems(_sourceInventory, _sourceIndex, targetInv, targetIndex);
            
            // 드래그 상태 초기화
            _sourceInventory = null;
            _sourceIndex = -1;
            _dragGhost.style.visibility = Visibility.Hidden;
            RefreshUI();
        }
    }
}