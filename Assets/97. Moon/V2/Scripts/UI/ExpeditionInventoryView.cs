using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Bond.Expedition;
using Bond.Persistence;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;
using VContainer;
using Random = UnityEngine.Random;

public class ExpeditionInventoryView : MonoBehaviour
{
    private ExpeditionInventory _expeditionInventory;
    [Inject] private InventoryTransferService _transferService;
    [Inject] private CharacterItemService _itemService;
    [Inject] private ExpeditionPayload _payload;
    
    private VisualElement _slotContainer;
    private List<VisualElement> _slots = new();
    private VisualElement _root;

    private async void Start()
    {
        _expeditionInventory = _payload.Supplies;
        
        // 1. 탐사 중에도 아이템 정보를 알아야 하므로 DB 로드 필요
        var conHandle = Addressables.LoadAssetAsync<ConsumableDataBaseSO>("ConsumableDataBase");
        var accHandle = Addressables.LoadAssetAsync<AccessoryDataBaseSO>("AccessoryDataBase");
        await System.Threading.Tasks.Task.WhenAll(conHandle.Task, accHandle.Task);

        // 2. "exp_inv" 파일만 로드
        LoadExpeditionInventory(conHandle.Result, accHandle.Result);
        
        _root = GetComponent<UIDocument>().rootVisualElement;
        _slotContainer = _root.Q<VisualElement>("expedition-container");

        // [개선] 마우스 커서가 하얀 영역(_slotContainer)을 완전히 벗어났을 때만 버리기 판정
        _root.RegisterCallback<PointerUpEvent>(evt => {
            if (_transferService.IsDragging) 
            {
                // 실제 마우스 커서 좌표가 하얀색 인벤토리 컨테이너 바운드 외부에 있을 때만 삭제
                if (!_slotContainer.worldBound.Contains(evt.position))
                {
                    var sourceInv = _transferService.CurrentSourceInventory;
                    int sourceIdx = _transferService.CurrentDraggingIndex;
                    if (sourceInv != null && sourceIdx != -1) 
                    {
                        var slotData = sourceInv.GetSlot(sourceIdx);
                        if (!slotData.IsEmpty) 
                        {
                            Debug.Log($"[아이템 파괴] 인벤토리 영역 밖에 드롭하여 {slotData.item.itemName}을(를) 버렸습니다.");
                            sourceInv.ClearSlot(sourceIdx);
                        }
                    }
                }
                else
                {
                    // 하얀 공간 안에서 슬롯이 아닌 곳에 놓았다면 아무 일도 하지 않고 복구(제자리)
                    Debug.Log("[드래그 취소] 인벤토리 내부 빈 공간에 드롭되어 제자리로 복구됩니다.");
                }
                _transferService.ResetDrag();
            }
            else if (_transferService.IsDraggingFromEquipment)
            {
                // 장비 슬롯에서 마우스를 떼었을 때도 하얀 영역 밖일 때만 파괴
                if (!_slotContainer.worldBound.Contains(evt.position))
                {
                    var hero = AdminTestTool.testHero;
                    if (hero != null)
                    {
                        _itemService.DiscardEquipment(hero, _transferService.SourceEquipmentSlotIndex);
                    }
                }
                _transferService.ResetEquipmentDrag();
            }
        }, TrickleDown.NoTrickleDown); // 슬롯에서 이벤트를 먹지 않았을 때(버블링 최종 단계) 실행

        _expeditionInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    public void ToggleWindow()
    {
        _root.style.display = (_root.style.display == DisplayStyle.None) ? DisplayStyle.Flex : DisplayStyle.None;
    }

    private void Update()
    {
        if (Keyboard.current.eKey.wasPressedThisFrame)
        {
            ToggleWindow();
        }
    }

    public void RefreshUI()
    {
        SyncSlots();
        for (int i = 0; i < _slots.Count; i++)
        {
            var data = _expeditionInventory.GetSlot(i);
            _slots[i].Clear();
            if (!data.IsEmpty)
            {
                var icon = new VisualElement();
                icon.style.backgroundImage = new StyleBackground(data.item.icon);
                icon.style.width = icon.style.height = Length.Percent(100);
                icon.pickingMode = PickingMode.Ignore;
                _slots[i].Add(icon);
            }
        }

        SaveExpeditionInventory();
    }

    private void SyncSlots()
    {
        while (_slots.Count < _expeditionInventory.Capacity)
        {
            int index = _slots.Count;
            var slot = new VisualElement();
            slot.AddToClassList("inventory-slot-base");

            slot.RegisterCallback<PointerDownEvent>(evt => {
                var data = _expeditionInventory.GetSlot(index);
                if (data.IsEmpty) return;
                
                if (evt.button == 0) { // 좌클릭 드래그 시작
                    _transferService.StartDrag(_expeditionInventory, index);
                }
                else if (evt.button == 1) { // 우클릭 자동 사용/장착
                    if (data.item.category == ItemCategory.Accessories) _itemService.AutoEquip(_expeditionInventory, index);
                    else if (data.item.category == ItemCategory.Consume) _itemService.UseItem(AdminTestTool.testHero, _expeditionInventory, index);
                }
            });

            slot.RegisterCallback<PointerUpEvent>(evt => {
                if (_transferService.IsDragging) {
                    _transferService.ExecuteDragDrop(_transferService.CurrentSourceInventory, _transferService.CurrentDraggingIndex, _expeditionInventory, index);
                    _transferService.ResetDrag();
                    evt.StopPropagation(); // 정상 슬롯에 안착했으므로 최상위 '영역 밖 버리기'로 이벤트가 흐르지 않게 차단
                }
                else if (_transferService.IsDraggingFromEquipment) {
                    var hero = AdminTestTool.testHero;
                    if (hero != null) {
                        _itemService.UnequipToInventorySlot(hero, _transferService.SourceEquipmentSlotIndex, _expeditionInventory, index);
                    }
                    _transferService.ResetEquipmentDrag();
                    evt.StopPropagation(); // 차단
                }
            });

            _slotContainer.Add(slot);
            _slots.Add(slot);
            
            _payload.SetSuplies(_expeditionInventory);
        }
    }
    
    private void LoadExpeditionInventory(params DataBaseSO[] dbs)
    {
        var save = new InventorySaveData("exp_inv");
        // SaveLoadSystem의 GetPath와 Key를 조합하여 경로 생성 (시스템 수정 없이 대응)
        string saveKey = save.Key;
        string path = Path.Combine(Application.dataPath, "Data", "Save", $"{saveKey}.json");

        if (File.Exists(path))
        {
            try 
            {
                SaveLoadSystem.Load(save);
                
                // 1. 기존 슬롯을 완전히 비우고 저장된 용량만큼 재생성
                _expeditionInventory.ClearAll(); 
                _expeditionInventory.ExpandStorage(save.capacity); 

                // 2. 아이템 복구
                foreach (var s in save.slots)
                {
                    BaseItem item = dbs.Select(db => db.GetSO<BaseItem>(s.id)).FirstOrDefault(i => i != null);
                    if (item != null) _expeditionInventory.AddItemAuto(item, s.count);
                }
                
                Debug.Log("ExpeditionInventory: 데이터 로드 성공");
            }
            catch (Exception e)
            {
                Debug.LogError($"ExpeditionInventory: 로드 중 오류 발생 - {e.Message}");
            }
        }
        else
        {
            Debug.Log("ExpeditionInventory: 기존 세이브 없음. 기본값으로 시작.");
        }
    }
    
    private void SaveExpeditionInventory()
    {
        // 저장할 데이터 객체 생성 (파일명: exp_inv)
        var save = new InventorySaveData("exp_inv");

        save.capacity = _expeditionInventory.Capacity; // 현재 용량 저장
        foreach (var slot in _expeditionInventory.GetAll())
        {
            if (!slot.IsEmpty)
                save.slots.Add(new InventorySaveData.SlotData { id = slot.item.id, count = slot.quantity });
        }

        // 세이브 시스템 실행
        SaveLoadSystem.Save(save);
    }
}