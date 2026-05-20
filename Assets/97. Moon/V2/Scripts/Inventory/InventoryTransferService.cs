using UnityEngine;

public class InventoryTransferService
{
    // [드래그 데이터 프로퍼티]
    public IInventory CurrentSourceInventory { get; private set; }
    public int CurrentDraggingIndex { get; private set; } = -1;
    public bool IsDragging => CurrentSourceInventory != null && CurrentDraggingIndex != -1;
    
    // ================= 기존 내부 메서드 수정 없이 상단/하단에 추가할 코드 =================
    public bool IsDraggingFromEquipment { get; private set; }
    public int SourceEquipmentSlotIndex { get; private set; } = -1;
    
    // 장비창에서 시작된 드래그인지 확인하기 위한 상태값들
    public bool IsEquipmentDragging { get; private set; }
    public int DraggingEquipmentSlotIndex { get; private set; }

    public void StartEquipmentDrag(int slotIndex)
    {
        IsEquipmentDragging = true;
        DraggingEquipmentSlotIndex = slotIndex;
        IsDraggingFromEquipment = true;
        SourceEquipmentSlotIndex = slotIndex;
    }

    public void ResetEquipmentDrag()
    {
        IsEquipmentDragging = false;
        DraggingEquipmentSlotIndex = -1;
        IsDraggingFromEquipment = false;
        SourceEquipmentSlotIndex = -1;
    }
// =================================================================================

    public void StartDrag(IInventory source, int index)
    {
        CurrentSourceInventory = source;
        CurrentDraggingIndex = index;
    }

    public void ResetDrag()
    {
        CurrentSourceInventory = null;
        CurrentDraggingIndex = -1;
    }
    
    // 기능
    public void ExecuteDragDrop(IInventory source, int sourceIdx, IInventory target, int targetIdx)
    {
        // [중요] 제자리 드롭(혹은 단순 클릭) 시 로직을 실행하지 않고 즉시 종료 (복사 방지)
        if (source == target && sourceIdx == targetIdx) return;

        // 1. 데이터 스냅샷 (완전 복사)
        var sSlot = source.GetSlot(sourceIdx);
        var tSlot = target.GetSlot(targetIdx);

        if (sSlot.IsEmpty) return;

        BaseItem sItem = sSlot.item;
        int sQty = sSlot.quantity;
        BaseItem tItem = tSlot.item; // 비어있을 수 있음
        int tQty = tSlot.quantity;
        
        // =========================================================================
        // [핵심 추가] 동일 아이템 합산(Merge) 예외 처리 레이어
        // 같은 아이템 위로 떨어뜨렸고, 타겟 슬롯의 제한치(expeditionSlotMax)에 여유가 있다면 합칩니다.
        // =========================================================================
        if (!tSlot.IsEmpty && sItem.id == tItem.id)
        {
            int slotMax = sItem.expeditionSlotMax;
            if (slotMax <= 0) slotMax = 1;

            // 타겟 슬롯에 담길 수 있는 남은 여유 공간 계산
            int roomInTarget = slotMax - tQty;

            if (roomInTarget > 0)
            {
                // 이동할 수 있는 수량 결정
                int moveQty = Mathf.Min(sQty, roomInTarget);

                // 1. 소스 슬롯 처리
                source.ClearSlot(sourceIdx); // 우선 싹 비우고
                if (sQty - moveQty > 0)
                {
                    // 다 못 옮기고 남은 잔량이 있다면 기존 자리에 남은 만큼만 다시 꽂아줌
                    source.AddItemAt(sourceIdx, sItem, sQty - moveQty);
                }

                // 2. 타겟 슬롯 처리
                target.ClearSlot(targetIdx); // 기존 타겟 슬롯을 잠깐 비운 뒤
                target.AddItemAt(targetIdx, tItem, tQty + moveQty); // 합산된 최종 수량으로 다시 꽂아줌

                Debug.Log($"[아이템 합산] {sItem.itemName} {moveQty}개가 합쳐졌습니다. 최종 수량: {tQty + moveQty}");
                return; // 합산 완료되었으므로 아래의 강제 스왑 로직을 타지 않고 종료
            }
        }
        // =========================================================================

        // 2. 인출(Withdraw): 두 슬롯을 완전히 비워서 인벤토리에서 '제거'된 상태로 만듦
        source.ClearSlot(sourceIdx);
        target.ClearSlot(targetIdx);

        // 3. 배치(Deposit)
        // 소스 아이템을 타겟에 배치
        int sRemain = target.AddItemAt(targetIdx, sItem, sQty);
        Debug.Log($"[드래그] {sItem.itemName} {sQty - sRemain}개 이동됨.");

        // 타겟 아이템(교체 대상)이 있었다면 소스(원래 위치)에 배치
        int tRemain = 0;
        if (tItem != null)
        {
            tRemain = source.AddItemAt(sourceIdx, tItem, tQty);
            Debug.Log($"[스왑] {sItem.itemName} <-> {tItem.itemName} 완료.");
        }

        // 4. 잔량 반환 (고향으로 복구)
        // [A] 소스 아이템의 잔량이 있다면 원래의 인벤토리(source)로 복귀
        if (sRemain > 0)
        {
            // AddItemAuto를 통해 빈자리나 기존 스택을 찾아 안전하게 들어감
            if (source.GetSlot(sourceIdx) != null) source.AddItemAuto(sItem, sRemain);
            else source.AddItemAt(sourceIdx, sItem, sRemain);
            
            Debug.Log($"[잔량반환] {sItem.itemName} {sRemain}개가 원래 인벤토리로 돌아갔습니다.");
        }

        // [B] 타겟 아이템의 잔량이 있다면 원래의 인벤토리(target)로 복귀
        if (tRemain > 0)
        {
            if(target.GetSlot(targetIdx) != null) target.AddItemAuto(tItem, tRemain);
            else target.AddItemAt(targetIdx, tItem, tRemain);
            Debug.Log($"[잔량반환] {tItem.itemName} {tRemain}개가 원래 인벤토리로 돌아갔습니다.");
        }
    }
    
    // 우클릭 1개 이동도 동일한 '인출 후 배치' 로직 적용
    public void MoveOneFromSlot(IInventory source, int sourceIdx, IInventory target)
    {
        var slot = source.GetSlot(sourceIdx);
        if (slot.IsEmpty) return;

        BaseItem item = slot.item;
        
        MoveFromSlot(source, sourceIdx, target, item, 1);
    }
    
    public void MoveAllFromSlot(IInventory source, int sourceIdx, IInventory target)
    {
        var slot = source.GetSlot(sourceIdx);
        if (slot.IsEmpty) return;
        
        BaseItem item = slot.item;
        
        MoveFromSlot(source, sourceIdx, target, item, item.expeditionSlotMax);
    }

    public void MoveFromSlot(IInventory source, int sourceIdx, IInventory target, BaseItem item, int quantity)
    {
        var slot = source.GetSlot(sourceIdx);
        if (slot.IsEmpty) return;
        
        source.RemoveFromSlot(sourceIdx, quantity); // 먼저 뺌

        int remain = target.AddItemAuto(item, quantity);
        
        if (remain == 0) // 이동 성공
        {
            Debug.Log($"[우클릭] {item.itemName} {quantity}개, {target.GetType().Name}으로 이동");
        }
        else
        {
            if (remain > 0) source.AddItemAt(sourceIdx, item, quantity); // 실패 시 복구
            Debug.LogWarning($"{target.GetType().Name}의 용량이 가득 찼습니다.");
        }
    }
}