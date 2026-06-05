using Bond.Expedition;
using Bond.Persistence;
using UnityEngine;
using VContainer;

public class ExpeditionResultService
{
    private ITotalInventory _totalInventory;
    private ExpeditionPayload _payload;
    private InventoryTransferService _transferService;
    private Roster _roster;

    [Inject]
    public ExpeditionResultService(
        ITotalInventory total,
        ExpeditionPayload payload,
        InventoryTransferService transfer,
        Roster roster)
    {
        _totalInventory = total;
        _payload = payload;
        _transferService = transfer;
        _roster = roster;
    }

    // 마을 씬 로드 후 호출될 메서드
    public void ProcessExpeditionReturn()
    {
        // 원정 동안 파티가 입은 HP/광기 등 세션 변경을 디스크에 플러시.
        _roster.SaveNow();

        if (_payload.Supplies == null) return;

        // 1. 탐사 인벤토리의 모든 아이템을 토탈 인벤토리로 이동
        for (int i = 0; i < _payload.Supplies.Capacity; i++)
        {
            var slot = _payload.Supplies.GetSlot(i);
            if (slot.IsEmpty) continue;

            // TransferService의 기존 로직을 활용해 토탈 인벤토리로 안전하게 전송
            // (초과 수량은 AddItemAuto 내부 로직에 의해 개척 데이터 등으로 처리될 것임)
            _totalInventory.AddItemAuto(slot.item, slot.quantity);
            _payload.Supplies.ClearSlot(i);
        }
        
        // 저장할 데이터 객체 생성 (파일명: exp_inv)
        var save = new InventorySaveData("exp_inv");

        save.capacity = _payload.Supplies.Capacity; // 현재 용량 저장
        foreach (var slot in _payload.Supplies.GetAll())
        {
            if (!slot.IsEmpty)
                save.slots.Add(new InventorySaveData.SlotData { id = slot.item.id, count = slot.quantity });
        }

        // 세이브 시스템 실행
        SaveLoadSystem.Save(save);
     

        // 2. 페이로드 초기화 (다음 탐사를 위해)
        Debug.Log("탐사 보급품이 토탈 인벤토리로 모두 회수되었습니다.");
    }
}