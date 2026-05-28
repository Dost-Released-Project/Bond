using UnityEngine;
using UnityEngine.EventSystems;

public class ConstructionSlot : MonoBehaviour
{
    public int slotIndex;
    
    // 🎯 [지정석 기획 핵심] 이 슬롯에 건설할 수 있는 유일한 건물 종류 지정
    [SerializeField] private BuildingType allowableType;
    public BuildingType AllowableType => allowableType;

    private void OnMouseDown()
    {
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Debug.Log($"슬롯 {slotIndex} 클릭됨! 지정된 건물 타입: {allowableType}");
    }
}