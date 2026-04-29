using UnityEngine;
using UnityEngine.EventSystems;

public class ConstructionSlot : MonoBehaviour
{
    public int slotIndex;

    private void OnMouseDown()
    {
        // UI를 클릭 중일 때는 월드의 블록이 클릭되지 않도록 방지
        if (EventSystem.current.IsPointerOverGameObject()) return;

        Debug.Log($"슬롯 {slotIndex} 클릭됨! 건설창을 엽니다.");
        
        // 씬에서 ConstructionUI를 찾아 오픈 (VContainer를 통해 주입받는 게 좋지만, 
        // 테스트를 위해 Find를 사용하거나 전역 싱글톤을 고려할 수 있습니다.)
        var ui = FindFirstObjectByType<ConstructionUI>();
        if (ui != null) ui.Open(slotIndex);
    }
}