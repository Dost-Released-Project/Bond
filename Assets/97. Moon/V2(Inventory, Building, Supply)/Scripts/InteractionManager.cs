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
        // 좌클릭: 건설 슬롯 열기 / 건물 상호작용(수급, 창고열기 등)
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            ExecuteRaycast(false); // false는 좌클릭 의미
        }

        // [추가] 우클릭: 건물 업그레이드 시도
        if (Mouse.current.rightButton.wasPressedThisFrame)
        {
            if (EventSystem.current.IsPointerOverGameObject()) return;
            ExecuteRaycast(true); // true는 우클릭 의미
        }
    }

    private void ExecuteRaycast(bool isRightClick)
    {
        Vector2 mousePos = Mouse.current.position.ReadValue();
        Ray ray = _mainCam.ScreenPointToRay(mousePos);
    
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            GameObject hitObj = hit.collider.gameObject;
            Debug.Log($"<color=yellow>[Raycast]</color> 감지됨: {hitObj.name}");
            
            var building = hit.collider.GetComponentInParent<BuildingObject>();
        
            if (building != null)
            {
                if (isRightClick) 
                    _settlementManager.UpgradeBuilding(building); // 우클릭 시 업그레이드
                else 
                    _settlementManager.OnBuildingClicked(building); // 좌클릭 시 상호작용
                return;
            }

            // 슬롯 클릭은 좌클릭일 때만 허용
            var slot = hit.collider.GetComponent<ConstructionSlot>();
            if (slot != null && !isRightClick)
            {
                _constructionUI.Open(slot.slotIndex);
            }
        }
    }
}