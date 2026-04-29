using UnityEngine;
using VContainer;

public class BuildingObject : MonoBehaviour
{
    public BuildingData Data { get; private set; }
    public int CurrentLevel { get; private set; } = 1;

    private SettlementManager _manager;

    public void Initialize(BuildingData data, SettlementManager manager)
    {
        Data = data;
        _manager = manager;
//        GetComponent<SpriteRenderer>().sprite = data.buildingSprite;
    }

    private void OnMouseDown() // 건물 클릭 시
    {
        if (UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;
        
        // SettlementManager에게 이 건물이 클릭되었음을 알림
        _manager.OnBuildingClicked(this);
    }

    public void Upgrade() => CurrentLevel++;
}