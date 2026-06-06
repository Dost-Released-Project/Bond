using Bond.Expedition;
using Bond.Persistence;
using UnityEngine;
using VContainer;

public class ExpeditionResultService
{
    private ITotalInventory _totalInventory;
    private ExpeditionPayload _payload;
    private ResourceManager _resourceManager;
    private Roster _roster;

    [Inject]
    public ExpeditionResultService(
        ITotalInventory total,
        ExpeditionPayload payload,
        ResourceManager resourceManager,
        Roster roster)
    {
        _totalInventory = total;
        _payload = payload;
        _resourceManager = resourceManager;
        _roster = roster;
    }

    // 마을 씬 로드 후 호출될 메서드
    public void ProcessExpeditionReturn()
    {
        // 원정 동안 파티가 입은 HP/광기 등 세션 변경을 디스크에 플러시.
        _roster.SaveNow();
        
        // =========================================================================
        // [신규 자원 정산] 탐사 전용 인벤토리에 박혀있던 누적 자원을 본진 자원에 주입
        // =========================================================================
        if (_payload.Supplies is ExpeditionInventory expInv)
        {
            int fAmt = expInv.AccumulatedFrontier;
            int wAmt = expInv.AccumulatedWood;
            int oAmt = expInv.AccumulatedOre;

            if (fAmt > 0) _resourceManager.AddResource(ResourceType.Frontier, fAmt);
            if (wAmt > 0) _resourceManager.AddResource(ResourceType.Wood, wAmt);
            if (oAmt > 0) _resourceManager.AddResource(ResourceType.Ore, oAmt);

            Debug.Log($"<color=orange>[원정 정산]</color> 탐사 자원 회수 완료 (개척: +{fAmt} / 목재: +{wAmt} / 광석: +{oAmt})");

            // 정산이 완전히 끝났으므로 가방의 임시 누적값들을 전부 0으로 마감 처리
            expInv.ClearAccumulatedResources();
        }

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