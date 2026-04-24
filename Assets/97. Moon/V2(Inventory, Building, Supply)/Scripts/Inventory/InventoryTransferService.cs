using UnityEngine;

public class InventoryTransferService
{
    // [우클릭] 아이템 1개씩 이동
    public void MoveOneItem(IInventory source, int sourceIdx, IInventory target)
    {
        var sourceSlot = source.GetSlot(sourceIdx);
        if (sourceSlot.IsEmpty) return;

        int initialCount = sourceSlot.quantity;
        BaseItem itemToMove = sourceSlot.item;

        // 타겟에 1개 추가 시도 (남은 개수가 0이면 성공한 것)
        int remain = target.AddItem(itemToMove, 1);
        
        if (remain == 0) // 이동 성공
        {
            source.TryRemoveItem(itemToMove.id, 1);
            Debug.Log($"[우클릭] {itemToMove.itemName} 1개 이동. {source.GetType().Name}에 {initialCount - 1}개 남음.");
        }
        else
        {
            Debug.LogWarning($"{target.GetType().Name}의 용량이 가득 찼습니다.");
        }
    }

    // [드래그] 드롭 시 실행될 메인 로직
    public void ExecuteDragDrop(IInventory source, int sourceIdx, IInventory target, int targetIdx)
    {
        if (source == target) 
        {
            // [요구사항 1] 같은 인벤토리 내 이동: 단순 위치 교체(Swap)
            // 이때는 '중복 체크' 로직을 타지 않고 데이터의 index만 바꿉니다.
            source.SwapSlots(sourceIdx, targetIdx);
        }
        else 
        {
            // [요구사항 2] 다른 인벤토리로 이동: 기존 전송 로직 (스택 제한 준수)
            var sourceSlot = source.GetSlot(sourceIdx);
            if (sourceSlot.IsEmpty) return;

            BaseItem item = sourceSlot.item;
            int amount = sourceSlot.quantity;

            // 타겟에 추가 시도 후 못 들어간 '잔량'을 반환받음
            int remaining = target.AddItem(item, amount);
            int actualMoved = amount - remaining;
            
            if (actualMoved > 0)
            {
                source.TryRemoveItem(item.id, actualMoved);
                Debug.Log($"[드래그] {item.itemName} {amount}개 중 {actualMoved}개 이동 성공. {remaining}개 남음.");
            }
        }
    }
}