using UnityEngine;

[CreateAssetMenu(fileName = "NewBuildingData", menuName = "Settle/Building Data")]
public class BuildingData : ScriptableObject
{
    public string buildingName;
    public BuildingType buildingType;
    
    [Header("건설 비용")]
    public int frontierCost; // 개척 데이터
    public int woodCost;     // 목재
    public int oreCost;      // 광물
    
    [TextArea]
    public string description;
}