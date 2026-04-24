using UnityEngine;
using UnityEngine.InputSystem; // 뉴 인풋 시스템 네임스페이스
using UnityEngine.EventSystems;
using VContainer;

public class InteractionManager : MonoBehaviour
{
    private SettlementManager _settlementManager;
    private ConstructionUI _constructionUI;
    private Camera _mainCam;

    [Inject]
    public void Construct(SettlementManager sm, ConstructionUI ui)
    {
        _settlementManager = sm;
        _constructionUI = ui;
    }

    private void Awake()
    {
        _mainCam = Camera.main;
    }

    void Update()
    {
        // 1. 마우스 왼쪽 버튼 클릭 시 (뉴 인풋 시스템 방식)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            // 2. UI 위에 마우스가 있다면 월드 클릭 무시
            if (EventSystem.current.IsPointerOverGameObject()) return;

            ExecuteRaycast();
        }
    }

    private void ExecuteRaycast()
    {
        // 마우스 현재 위치 읽기
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCam.ScreenPointToRay(mousePos);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            GameObject hitObj = hit.collider.gameObject;
            Debug.Log($"<color=yellow>[Raycast]</color> 감지됨: {hitObj.name}");

            // A. 건설된 건물인지 확인 (BuildingObject는 자식에 있을 수 있으므로 Parent까지 확인)
            var building = hitObj.GetComponentInParent<BuildingObject>();
            if (building != null)
            {
                _settlementManager.OnBuildingClicked(building);
                return;
            }

            // B. 빈 건설 슬롯인지 확인
            var slot = hitObj.GetComponent<ConstructionSlot>();
            if (slot != null)
            {
                _constructionUI.Open(slot.slotIndex);
                return;
            }
        }
    }
}