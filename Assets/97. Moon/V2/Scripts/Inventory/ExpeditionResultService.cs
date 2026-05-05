using Bond.Expedition;
using UnityEngine;
using VContainer;

public class ExpeditionResultService
{
    private ITotalInventory _totalInventory;
    private ExpeditionPayload _payload;
    private InventoryTransferService _transferService;

    [Inject]
    public ExpeditionResultService(
        ITotalInventory total, 
        ExpeditionPayload payload, 
        InventoryTransferService transfer)
    {
        _totalInventory = total;
        _payload = payload;
        _transferService = transfer;
    }

    // 마을 씬 로드 후 호출될 메서드
    public void ProcessExpeditionReturn()
    {
        var supplies = _payload.Supplies;
        if (supplies == null) return;

        // 1. 탐사 인벤토리의 모든 아이템을 토탈 인벤토리로 이동
        for (int i = 0; i < supplies.Capacity; i++)
        {
            var slot = supplies.GetSlot(i);
            if (slot.IsEmpty) continue;

            // TransferService의 기존 로직을 활용해 토탈 인벤토리로 안전하게 전송
            // (초과 수량은 AddItemAuto 내부 로직에 의해 개척 데이터 등으로 처리될 것임)
            _totalInventory.AddItemAuto(slot.item, slot.quantity);
            supplies.ClearSlot(i);
        }

        // 2. 페이로드 초기화 (다음 탐사를 위해)
        Debug.Log("탐사 보급품이 토탈 인벤토리로 모두 회수되었습니다.");
    }
}