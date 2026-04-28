using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using VContainer;
using System.Collections.Generic;

public class InteractionManager : MonoBehaviour
{
    private SettlementManager _settlementManager;
    private ConstructionUI _constructionUI;
    private InventoryView _inventoryView;
    private ExpeditionInventoryView _expeditionView;
    private AccessoryBagView _accessoryBagView;
    private IEnumerable<EquipmentSlotUI> _equipSlots; // 여러 슬롯을 리스트로 받음

    private Camera _mainCam;

    [Inject]
    public void Construct(
        SettlementManager sm, 
        ConstructionUI ui, 
        InventoryView iv, 
        ExpeditionInventoryView ev, 
        AccessoryBagView av,
        IEnumerable<EquipmentSlotUI> slots) // VContainer가 모든 인스턴스를 찾아 넣어줌
    {
        _settlementManager = sm;
        _constructionUI = ui;
        _inventoryView = iv;
        _expeditionView = ev;
        _accessoryBagView = av;
        _equipSlots = slots;
    }

    private void Awake() => _mainCam = Camera.main;

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

        // UI 3종 토글 (각 뷰의 ToggleWindow 호출)
        //if (kb.iKey.wasPressedThisFrame) _inventoryView.ToggleWindow(!InventoryView.IsWindowActive);
        if (kb.eKey.wasPressedThisFrame) _expeditionView.ToggleWindow();
        if (kb.bKey.wasPressedThisFrame) _accessoryBagView.ToggleWindow();
        if (kb.uKey.wasPressedThisFrame) foreach(var s in _equipSlots) s.ToggleWindow();

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