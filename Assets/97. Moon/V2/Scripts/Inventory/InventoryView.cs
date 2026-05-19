using System;
using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Bond.Persistence;
using UnityEngine.AddressableAssets;
using VContainer;

public class InventoryView : MonoBehaviour
{
    private VisualElement _root, _totalGrid, _expeditionGrid, _dragGhost;
    private List<VisualElement> _totalSlotElements = new(), _expeditionSlotElements = new();
    private TextField _searchField;
    private ScrollView _totalScroll;

    private ITotalInventory _totalInventory;
    private InventoryTransferService _transferService;
    private InventoryUIService _uiService;     // 추가된 서비스
    private ExpeditionResultService _expeditionResultService;
    
    private string _currentSearch = "";
    private ItemCategory? _currentFilter = null;
    
    private VisualElement _tooltip; // 상세 정보를 띄울 최상위 레이어

    [Inject]
    public void Construct(ITotalInventory total, InventoryTransferService service, 
        InventoryUIService uiService, ExpeditionResultService expeditionResultService)
    {
        _totalInventory = total;
        _transferService = service; _uiService = uiService;
        _expeditionResultService = expeditionResultService;
    }

    private async void Start()
    {
        // 1. "total_inv" 파일만 로드
        await LoadTotalInventory();
        
        // 2. ID로 테스트 아이템 추가.
        // 소모품 추가
        _totalInventory.AddItemId("07000000", 5);
        _totalInventory.AddItemId("07010000", 5);
        _totalInventory.AddItemId("07020000", 5);
        _totalInventory.AddItemId("07030000", 5);
        _totalInventory.AddItemId("07040000", 5);
        // 장신구 추가.
        _totalInventory.AddItemId("08000000", 1);
        _totalInventory.AddItemId("08010000", 1);
        _totalInventory.AddItemId("08020000", 1);
        _totalInventory.AddItemId("08030000", 1);
        
        // 3. 탐사 후 타운으로 넘어올 때, 탐사 인벤토리 아이템 모두 토탈 인벤토리로 이동. 파일 로드 이후 적용해야지 적용됨
        _expeditionResultService.ProcessExpeditionReturn();
        SetupUI();

        ToggleWindow(false);
    }

    private async Task LoadTotalInventory()
    {
        var conHandle = Addressables.LoadAssetAsync<ConsumableDataBaseSO>("ConsumableDataBase");
        var accHandle = Addressables.LoadAssetAsync<AccessoryDataBaseSO>("AccessoryDataBase");
        
        // [중요] 두 데이터베이스가 하드디스크/메모리에서 완전히 로드될 때까지 기다림
        await Task.WhenAll(conHandle.Task, accHandle.Task);
        
        DataBaseSO[] dbs = { conHandle.Result, accHandle.Result };
        
        // 로드 시도
        var save = new InventorySaveData("total_inv");
        // SaveLoadSystem의 GetPath와 Key를 조합하여 경로 생성 (시스템 수정 없이 대응)
        string saveKey = save.Key;
        string path = Path.Combine(Application.dataPath, "Data", "Save", $"{saveKey}.json");

        if (File.Exists(path))
        {
            try 
            {
                SaveLoadSystem.Load(save);
                
                // 1. 기존 슬롯을 완전히 비우고 저장된 용량만큼 재생성
                _totalInventory.ClearAll(); 
                _totalInventory.ExpandStorage(save.capacity); 

                // 2. 아이템 복구
                foreach (var s in save.slots)
                {
                    BaseItem item = dbs.Select(db => db.GetSO<BaseItem>(s.id)).FirstOrDefault(i => i != null);
                    if (item != null) _totalInventory.AddItemAuto(item, s.count);
                }
                
                Debug.Log("TotalInventory: 데이터 로드 성공");
            }
            catch (Exception e)
            {
                Debug.LogError($"TotalInventory: 로드 중 오류 발생 - {e.Message}");
            }
        }
        else
        {
            Debug.Log("TotalInventory: 기존 세이브 없음. 기본값으로 시작.");
        }
    }
    
    private void SaveTotalInventory()
    {
        // 1. 저장할 데이터 객체 생성 (파일명: total_inv)
        var save = new InventorySaveData("total_inv");
        save.capacity = _totalInventory.Capacity;
        foreach (var slot in _totalInventory.GetAll())
        {
            if (!slot.IsEmpty)
                save.slots.Add(new InventorySaveData.SlotData { id = slot.item.id, count = slot.quantity });
        }

        // 3. 세이브 시스템 실행
        SaveLoadSystem.Save(save);
    }
    
    private void SetupUI()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _totalGrid = _root.Q<VisualElement>("total-grid");
        _totalScroll = _root.Q<ScrollView>("total-inventory-grid");

        // 1. 툴팁 & 드래그 고스트 초기화
        _tooltip = CreateOverlayElement("inventory-tooltip");
        _dragGhost = CreateOverlayElement(null, 60);
        _root.RegisterCallback<PointerMoveEvent>(OnPointerMove);

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
        if (slot.IsEmpty || _uiService.CurrentSourceInventory != null) return;

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

        _tooltip.style.left = position.x + 20;
        _tooltip.style.top = position.y + 20;
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
            _uiService.StartDrag(inv, index, inv.GetSlot(index).item.icon, _dragGhost, evt.position, new Vector2(30, 30));
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
        
        SaveTotalInventory();
    }

    private void SetFilter(ItemCategory? cat) { _currentFilter = cat; RefreshUI(); }

    private void HideTooltip() => _tooltip.style.visibility = Visibility.Hidden;
}