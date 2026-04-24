using UnityEngine;

public class InventoryTransferService
{
    // 드래그 앤 드롭 스왑/이동
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
            source.AddItemAt(sourceIdx, sItem, sRemain);
            Debug.Log($"[잔량반환] {sItem.itemName} {sRemain}개가 원래 인벤토리로 돌아갔습니다.");
        }

        // [B] 타겟 아이템의 잔량이 있다면 원래의 인벤토리(target)로 복귀
        if (tRemain > 0)
        {
            target.AddItemAt(targetIdx, tItem, tRemain);
            Debug.Log($"[잔량반환] {tItem.itemName} {tRemain}개가 원래 인벤토리로 돌아갔습니다.");
        }
    }
    
    // 우클릭 1개 이동도 동일한 '인출 후 배치' 로직 적용
    public void MoveOneFromSlot(IInventory source, int sourceIdx, IInventory target)
    {
        var slot = source.GetSlot(sourceIdx);
        if (slot.IsEmpty) return;

        BaseItem item = slot.item;
        source.RemoveFromSlot(sourceIdx, 1); // 먼저 뺌

        int remain = target.AddItemAuto(item, 1);
        
        if (remain == 0) // 이동 성공
        {
            Debug.Log($"[우클릭] {item.itemName} 1개, {target.GetType().Name}으로 이동");
        }
        else
        {
            if (remain > 0) source.AddItemAt(sourceIdx, item, 1); // 실패 시 복구
            Debug.LogWarning($"{target.GetType().Name}의 용량이 가득 찼습니다.");
        }
    }
}