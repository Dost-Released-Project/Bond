using UnityEngine;

public interface ISupplyManager
{
    void RequestReinforcement(); // 증원
    void RequestSupply(SupplyType type); // 일반/특수 보급
    int GetRequiredData(SupplyType type); // 필요 개척 데이터(Log) 확인
}