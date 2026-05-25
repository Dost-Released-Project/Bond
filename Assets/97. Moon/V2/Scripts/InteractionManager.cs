using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using VContainer;
using System.Collections.Generic;

public class InteractionManager : MonoBehaviour
{
    private SettlementManager _settlementManager;
    private ConstructionUI _constructionUI;

    private Camera _mainCam;

    [Inject]
    public void Construct(
        SettlementManager sm, 
        ConstructionUI ui
        ) // VContainer가 모든 인스턴스를 찾아 넣어줌
    {
        _settlementManager = sm;
        _constructionUI = ui;
    }

    private void Awake() => _mainCam = Camera.main;

    void Update()
    {
        // 마우스 상호작용
        if (Mouse.current.leftButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
            ExecuteRaycast(false);
        if (Mouse.current.rightButton.wasPressedThisFrame && !EventSystem.current.IsPointerOverGameObject())
            ExecuteRaycast(true);
    }

    private void ExecuteRaycast(bool isRightClick)
    {
        Ray ray = _mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            var building = hit.collider.GetComponentInParent<BuildingObject>();
            if (building != null)
            {
                if (isRightClick) _settlementManager.UpgradeBuilding(building);
                else _settlementManager.OnBuildingClicked(building);
                return;
            }

            var slot = hit.collider.GetComponent<ConstructionSlot>();
            if (slot != null && !isRightClick) _constructionUI.Open(slot.slotIndex);
        }
    }
}