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
    private IExpeditionInventory _expeditionInventory;
    private InventoryTransferService _transferService;
    private InventoryUIService _uiService;     // 추가된 서비스
    private ExpeditionResultService _expeditionResultService;
    
    private string _currentSearch = "";
    private ItemCategory? _currentFilter = null;

    [Inject]
    public void Construct(ITotalInventory total, IExpeditionInventory expedition, 
        InventoryTransferService service, InventoryUIService uiService, ExpeditionResultService expeditionResultService)
    {
        _totalInventory = total; _expeditionInventory = expedition;
        _transferService = service; _uiService = uiService;
        _expeditionResultService = expeditionResultService;
    }

    private async void Start()
    {
        // 1. 아이템 DB 로드
        var conHandle = Addressables.LoadAssetAsync<ConsumableDataBaseSO>("ConsumableDataBase");
        var accHandle = Addressables.LoadAssetAsync<AccessoryDataBaseSO>("AccessoryDataBase");
        await System.Threading.Tasks.Task.WhenAll(conHandle.Task, accHandle.Task);
        
        var accDB = accHandle.Result;
        var conDB = conHandle.Result;
        
        // 2. DB의 GetSO 함수를 사용하여 아이템 추가
        // 소모품 추가
        AddInventoryItem(conDB, "07000000", 5);
        AddInventoryItem(conDB, "07010000", 5);
        AddInventoryItem(conDB, "07020000", 5);
        AddInventoryItem(conDB, "07030000", 5);
        AddInventoryItem(conDB, "07040000", 5);
    
        // 악세서리 추가
        AddInventoryItem(accDB, "08000000", 1);
        AddInventoryItem(accDB, "08010000", 1);
        AddInventoryItem(accDB, "08020000", 1);
        AddInventoryItem(accDB, "08030000", 1);        

        // 2. "total_inv" 파일만 로드
        LoadTotalInventory(conHandle.Result, accHandle.Result);
        
        _expeditionResultService.ProcessExpeditionReturn();
        SetupUI();

        ToggleWindow(false);
    }

    private void AddInventoryItem(DataBaseSO db, string id, int count)
    {
        var item = db.GetSO<BaseItem>(id); // 작성하신 GetSO 활용!
        if (item != null)
            _totalInventory.AddItemAuto(item, count);
    }

    private void LoadTotalInventory(params DataBaseSO[] dbs)
    {
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
                
                _totalInventory.ClearAll(); // 복구 전 초기화
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

        // 2. 현재 인벤토리의 모든 슬롯을 순회하며 데이터 추출
        foreach (var slot in _totalInventory.GetAll())
        {
            if (!slot.IsEmpty)
            {
                save.slots.Add(new InventorySaveData.SlotData 
                { 
                    id = slot.item.id, 
                    count = slot.quantity 
                });
            }
        }

        // 3. 세이브 시스템 실행
        SaveLoadSystem.Save(save);
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
        var expeditionVisible = _expeditionInventory.GetFilteredIndices("", null);

        UpdateGrid(_totalSlotElements, _totalInventory, totalVisible);
        UpdateGrid(_expeditionSlotElements, _expeditionInventory, expeditionVisible);
        
        SaveTotalInventory();
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