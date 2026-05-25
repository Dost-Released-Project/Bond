using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using VContainer;
using System.Collections.Generic;

public class InteractionManager : MonoBehaviour
{
    private BuildingObject _lastHoveredBuilding;

    [Inject] private SettlementManager _settlementManager;
    [Inject] private ConstructionUI _constructionUI; // 상태 체크를 위한 주입

    private Camera _mainCam;
    
    private void Awake() => _mainCam = Camera.main;
    
    private void Update()
    {
        if (_mainCam == null || Mouse.current == null || _constructionUI == null) return;

        // 💥 [추가 조건 저격] 만약 건설/사용/업글 중 어떤 팝업이든 하나라도 열려있다면?
        // 마우스 호버 이펙트를 일시 해제하고, 새로운 클릭 상호작용 레이 연산을 완전히 스킵합니다!
        if (_constructionUI.IsAnyPopupOpen)
        {
            if (_lastHoveredBuilding != null)
            {
                if (_lastHoveredBuilding.Visuals != null) _lastHoveredBuilding.Visuals.ResetHoverState();
                _lastHoveredBuilding = null;
            }
            return; 
        }

        // 유니티 자체 UI(인벤토리 등) 위를 지나갈 때도 예외 처리 방어막
        if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
        {
            if (_lastHoveredBuilding != null)
            {
                if (_lastHoveredBuilding.Visuals != null) _lastHoveredBuilding.Visuals.ResetHoverState();
                _lastHoveredBuilding = null;
            }
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCam.ScreenPointToRay(mousePosition);

        BuildingObject currentHitBuilding = null;
        ConstructionSlot currentHitSlot = null;

        // 🎯 딱 1번의 전역 레이캐스트 난사로 타깃 검출
        if (Physics.Raycast(ray, out RaycastHit hit, 500f))
        {
            currentHitBuilding = hit.collider.GetComponentInParent<BuildingObject>();
            currentHitSlot = hit.collider.GetComponent<ConstructionSlot>();
        }

        // 📊 [전역 호버 연산 분리 통합]
        if (_lastHoveredBuilding != currentHitBuilding)
        {
            if (_lastHoveredBuilding != null && _lastHoveredBuilding.Visuals != null)
            {
                _lastHoveredBuilding.Visuals.ResetHoverState();
            }

            if (currentHitBuilding != null && currentHitBuilding.Visuals != null)
            {
                currentHitBuilding.Visuals.TriggerHoverState(mousePosition);
            }
            _lastHoveredBuilding = currentHitBuilding;
        }

        // 🖱️ [전역 클릭 연산 분리 통합]
        if (Mouse.current.leftButton.wasPressedThisFrame)
        {
            if (currentHitBuilding != null)
            {
                _settlementManager.OnBuildingClicked(currentHitBuilding);
            }
            else if (currentHitSlot != null)
            {
                // UI 설계 단계 진입 파이프라인 연동
                _constructionUI.OpenConstruction(currentHitSlot.slotIndex, currentHitSlot.AllowableType);
            }
        }
        else if (Mouse.current.rightButton.wasPressedThisFrame && currentHitBuilding != null)
        {
            // 우클릭 즉시 강화 대신 리더님 기획대로 업그레이드 UI 준비창 호출 예정
            _constructionUI.OpenUpgrade(currentHitBuilding);
        }
    }
}