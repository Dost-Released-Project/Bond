public class InventoryTransferService
{
    private readonly ITotalInventory _totalInventory;
    private readonly IExpeditionInventory _expeditionInventory;

    public InventoryTransferService(ITotalInventory total, IExpeditionInventory expedition)
    {
        _totalInventory = total;
        _expeditionInventory = expedition;
    }

    // 드래그 앤 드롭: 교체 로직
    public void SwapItems(IInventory source, int sourceIdx, IInventory target, int targetIdx)
    {
        var sourceSlot = source.GetSlot(sourceIdx);
        var targetSlot = target.GetSlot(targetIdx);

        // 단순 교환 (Swap)
        var temp = new InventorySlot { item = sourceSlot.item, quantity = sourceSlot.quantity };
        sourceSlot.item = targetSlot.item;
        sourceSlot.quantity = targetSlot.quantity;
        
        targetSlot.item = temp.item;
        targetSlot.quantity = temp.quantity;
    }

    // 우클릭 이동: 가득 차면 이동 불가
    public bool TryMoveItem(IInventory source, int sourceIdx, IInventory target)
    {
        var slot = source.GetSlot(sourceIdx);
        if (slot.IsEmpty) return false;

        // 타겟의 빈 공간 확인 및 추가 시도 (가득 차면 false 반환하도록 설계 필요)
        // 여기서는 타겟 인벤토리의 AddItem 로직을 활용
        target.AddItem(slot.item, slot.quantity);
        // ... (성공 여부에 따른 원본 삭제 로직 생략)
        return true;
    }
}