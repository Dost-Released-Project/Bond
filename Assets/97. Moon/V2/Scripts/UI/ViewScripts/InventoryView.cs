using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bond.Expedition;
using Bond.Persistence;
using UnityEngine.AddressableAssets;
using VContainer;

public class InventoryView : MonoBehaviour
{
    private VisualElement _root, _totalGrid, _expeditionGrid;
    private List<VisualElement> _totalSlotElements = new();
    private TextField _searchField;
    private ScrollView _totalScroll;

    private TotalInventory _totalInventory;
    private InventoryTransferService _transferService;
    private ExpeditionResultService _expeditionResultService;
    [Inject] private ExpeditionPayload _payload;
    
    private string _currentSearch = "";
    private ItemCategory? _currentFilter = null;
    
    private VisualElement _tooltip; // 상세 정보를 띄울 최상위 레이어

    [Inject]
    public void Construct(TotalInventory total, InventoryTransferService service, ExpeditionResultService expeditionResultService)
    {
        _totalInventory = total;
        _transferService = service;
        _expeditionResultService = expeditionResultService;
    }

    private void Start()
    {
        // 1. "total_inv" 파일 로드
        string savePath = Path.Combine(Application.dataPath, "Data", "Save", "total_inv.json");
        bool isFirstStart = !File.Exists(savePath);
        
        _totalInventory.LoadTotalInventory();
    
        // 2. [개혁] 세이브 파일이 없는 순수 신규 게임("새로하기")일 때만 단 1회 테스트 아이템 추가
        if (isFirstStart)
        {
            Debug.Log("<color=yellow>[최초 실행]</color> 새 게임 시작 초기 아이템을 1회 한정 지급합니다.");

            // 소모품 추가
            _totalInventory.AddItemId("07000000", 3);
            _totalInventory.AddItemId("07010000", 3);
            _totalInventory.AddItemId("07030000", 1);
            _totalInventory.AddItemId("07040000", 1);

            // 지급된 상태를 즉시 파일로 구워내어 다음 재실행/복귀 시 다시 지급되는 현상 방어
            _totalInventory.SaveTotalInventory(); 
        }
        else
        {
            Debug.Log("TotalInventory: 기존 세이브 데이터가 존재하므로 초기 아이템 지급을 스킵합니다.");
        }
        
        _payload.Supplies.InitAndLoad();
    
        // 3. 탐사 후 타운으로 넘어올 때, 탐사 인벤토리 아이템 모두 토탈 인벤토리로 이동. 파일 로드 이후 적용해야지 적용됨
        _expeditionResultService.ProcessExpeditionReturn();
        SetupUI();

        ToggleWindow(false);
    }
    
    private void SetupUI()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _totalGrid = _root.Q<VisualElement>("total-grid");
        _totalScroll = _root.Q<ScrollView>("total-inventory-grid");

        // 1. 툴팁 & 드래그 고스트 초기화
        _tooltip = CreateOverlayElement("inventory-tooltip");

        // 2. 버튼 및 검색 필드 (기존 로직 유지)
        _searchField = _root.Q<TextField>("inventory-search");
        _searchField?.RegisterValueChangedCallback(evt => { _currentSearch = evt.newValue; RefreshUI(); });
        _root.Q<Button>("btn-sort")?.RegisterCallback<ClickEvent>(evt => { _totalInventory.SortById(); RefreshUI(); });
        _root.Q<Button>("btn-filter-consumable")?.RegisterCallback<ClickEvent>(evt => SetFilter(ItemCategory.Consume));
        _root.Q<Button>("btn-filter-accessory")?.RegisterCallback<ClickEvent>(evt => SetFilter(ItemCategory.Accessories));
        _root.Q<Button>("btn-filter-all")?.RegisterCallback<ClickEvent>(evt => SetFilter(null));
        _root.Q<Button>("btn-close")?.RegisterCallback<ClickEvent>(evt => ToggleWindow(false));

        _totalInventory.OnChanged += RefreshUI;
        SyncSlotCount(_totalGrid, _totalInventory.Capacity, _totalSlotElements, _totalInventory);
        
        SetFilter(null);
    }

    // 오버레이 요소 생성 헬퍼 (코드 단축)
    private VisualElement CreateOverlayElement(string className, float size = -1)
    {
        var ve = new VisualElement { style = { position = Position.Absolute, visibility = Visibility.Hidden } };
        ve.pickingMode = PickingMode.Ignore;
        if (!string.IsNullOrEmpty(className)) ve.AddToClassList(className);
        if (size > 0) { ve.style.width = size; ve.style.height = size; }
        _root.Add(ve);
        return ve;
    }

    private void SyncSlotCount(VisualElement container, int targetCount, List<VisualElement> list, IInventory inv)
    {
        while (list.Count < targetCount)
        {
            int index = list.Count;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-list-item");
            
            // [복구] 초기 드래그 앤 드롭 로직 그대로 사용
            slot.RegisterCallback<PointerDownEvent>(evt => OnPointerDown(evt, inv, index));
            slot.RegisterCallback<PointerUpEvent>(evt => OnPointerUp(evt, inv, index));
            
            slot.RegisterCallback<MouseEnterEvent>(evt => ShowTooltip(inv.GetSlot(index), evt.mousePosition));
            slot.RegisterCallback<MouseLeaveEvent>(evt => HideTooltip());

            container.Add(slot);
            list.Add(slot);
        }
    }
    
    private void UpdateGrid(List<VisualElement> elements, IInventory inv, IEnumerable<int> visibleIndices)
    {
        var visibleSet = new HashSet<int>(visibleIndices);
        for (int i = 0; i < elements.Count; i++)
        {
            var visual = elements[i]; visual.Clear();
            if (!visibleSet.Contains(i)) { visual.style.display = DisplayStyle.None; continue; }

            visual.style.display = DisplayStyle.Flex;
            var data = inv.GetSlot(i);
            if (data.IsEmpty) continue;

            // 아이콘
            var icon = new VisualElement { style = { width = 55, height = 55, backgroundImage = new StyleBackground(data.item.icon) } };
            // 배경 크기를 조절하는 표준 방식 (보통 ScaleToFit 대신 사용)
            icon.style.backgroundSize = new BackgroundSize(BackgroundSizeType.Contain);
            visual.Add(icon);

            // 텍스트 그룹 (이름 + 타입)
            var textGroup = new VisualElement { style = { flexGrow = 1, marginLeft = 15, justifyContent = Justify.Center } };
            textGroup.Add(new Label(data.item.DisplayName) { style = { color = Color.white, fontSize = 16, unityFontStyleAndWeight = FontStyle.Bold } });
            textGroup.Add(new Label(data.item.category.ToString()) { style = { color = new Color(0.7f, 0.7f, 0.7f), fontSize = 12 } });
            visual.Add(textGroup);

            // 수량
            visual.Add(new Label($"{data.quantity} / {data.item.totalGlobalMax}") { 
                style = { color = Color.white, width = 70, unityTextAlign = TextAnchor.MiddleRight } 
            });
        }
    }

    private void ShowTooltip(InventorySlot slot, Vector2 position)
    {
        if (slot.IsEmpty || _transferService.CurrentSourceInventory != null) return;

        _tooltip.Clear();
        _tooltip.Add(new Label(slot.item.DisplayName) { name = "title" });
        _tooltip.Q<Label>("title").AddToClassList("tooltip-title");

        AddTooltipLabel($"{(string.IsNullOrEmpty(slot.item.Description) ? "내용 없음" : slot.item.Description)}");
        AddTooltipLabel($"최대 저장 개수: {slot.item.totalGlobalMax}");
        AddTooltipLabel($"가방 제한: {slot.item.expeditionSlotMax}"); // [복구] 누락 데이터 추가

        if (slot.item is ConsumableItem con) AddTooltipLabel($"회복량: <color=#00FF00>{con.healValue}</color>");
        if (slot.item is AccessoryItem acc)
        {
            AddTooltipLabel("\n[장착 효과]");
            foreach (var effect in acc.specialEffects)
            {
                var valueMode = effect.mode == ModifierMode.Flat ? $"{effect.value} 증가" : $"{effect.value:P1} 증가";
                AddTooltipLabel($"- {effect.name}: {effect.type} + {valueMode}");
            }
        }

        // =========================================================================
        // 🖥️ [툴팁 스크린 이탈 방지] 
        // =========================================================================
        float tooltipWidth = 250f;  
        float tooltipHeight = 150f; 

        // 가로 제한 연산
        float finalX = position.x + 20f;
        if (finalX + tooltipWidth > Screen.width)
        {
            finalX = position.x - tooltipWidth - 20f;
        }

        // 세로 제한 연산
        float finalY = position.y + 20f;
        if (finalY + tooltipHeight > Screen.height)
        {
            finalY = position.y - tooltipHeight - 20f;
        }

        // 벽 뚫기 방어 최소값 보정
        if (finalX < 5f) finalX = 5f;
        if (finalY < 5f) finalY = 5f;

        _tooltip.style.left = finalX; 
        _tooltip.style.top = finalY;
        _tooltip.style.visibility = Visibility.Visible; 
        _tooltip.BringToFront();
    }

    private void AddTooltipLabel(string text)
    {
        var label = new Label(text);
        label.AddToClassList("tooltip-text");
        _tooltip.Add(label);
    }

    // 드래그 로직 (초기 버전 복구)
    private void OnPointerDown(PointerDownEvent evt, IInventory inv, int index)
    {
        if (evt.button == 0 && !inv.GetSlot(index).IsEmpty)
        {
            HideTooltip();
            _transferService.StartDrag(inv, index);
        }
    }

    private void OnPointerUp(PointerUpEvent evt, IInventory targetInv, int targetIndex)
    {
        if (_transferService.CurrentSourceInventory != null)
        {
            _transferService.ExecuteDragDrop(_transferService.CurrentSourceInventory, _transferService.CurrentDraggingIndex, targetInv, targetIndex);
            _transferService.ResetDrag();
        }
    }
    
    public void ToggleWindow(bool show)
    {
        _root.Q<VisualElement>("inventory-container").style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
        if (show) { if (_totalScroll != null) _totalScroll.scrollOffset = Vector2.zero; RefreshUI();}
    }

    public void RefreshUI()
    {
        SyncSlotCount(_totalGrid, _totalInventory.Capacity, _totalSlotElements, _totalInventory);

        var totalVisible = _totalInventory.GetFilteredIndices(_currentSearch, _currentFilter);

        UpdateGrid(_totalSlotElements, _totalInventory, totalVisible);
        
        _totalInventory.SaveTotalInventory(); 
    }

    private void SetFilter(ItemCategory? cat) 
    { 
        _currentFilter = cat; 
        RefreshUI(); 

        // 💥 현재 누른 탭 버튼은 비활성화하고, 나머지 탭은 활성화하여 락 연출 수용
        _root.Q<Button>("btn-filter-all")?.SetEnabled(cat != null);
        _root.Q<Button>("btn-filter-consumable")?.SetEnabled(cat != ItemCategory.Consume);
        _root.Q<Button>("btn-filter-accessory")?.SetEnabled(cat != ItemCategory.Accessories);
    }

    private void HideTooltip() => _tooltip.style.visibility = Visibility.Hidden;
}