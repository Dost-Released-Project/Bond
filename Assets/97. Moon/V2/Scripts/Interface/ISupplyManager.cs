public interface ISupplyManager
{
    /// <summary> 개척 데이터를 소모하여 영웅 증원 </summary>
    void RequestReinforcement(); 
    
    /// <summary> 개척 데이터를 소모하여 일반/특수 아이템 보급 </summary>
    void RequestSupply(SupplyType type); 
    
    /// <summary> 보급 타입에 따른 필요 비용 반환 </summary>
    int GetRequiredData(SupplyType type); 
}