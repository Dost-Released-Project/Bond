using UnityEngine;

// 전체 인벤토리: 개척 데이터 전환 및 슬롯 확장
public interface ITotalInventory : IInventory
{
    void ProcessExcessToLog(BaseItem item, int quantity);
}
