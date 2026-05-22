using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections.Generic;
using UnityEngine.InputSystem;

public class MouseDebugTracker : MonoBehaviour
{
    private Camera _mainCam;

    private void Start()
    {
        _mainCam = Camera.main;
        if (_mainCam == null)
        {
            Debug.LogError("<color=red>[디버그 ERROR]</color> 씬에 'MainCamera' 태그가 붙은 카메라가 없습니다!");
        }
    }

    private void Update()
    {
        // 💥 매 프레임 마우스 커서 아래에 도대체 "누가" 있는지 강제로 레이를 쏴서 실시간 추적합니다.
        if (_mainCam == null) return;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = Mouse.current.position.ReadValue();

        // 1. 유니티 UI / UI Toolkit 레이캐스트 결과 추적
        List<RaycastResult> results = new List<RaycastResult>();
        if (EventSystem.current != null)
        {
            EventSystem.current.RaycastAll(eventData, results);
            if (results.Count > 0)
            {
                Debug.Log($"<color=yellow>[UI 차단 검출]</color> 마우스가 UI 요소 <b>{results[0].gameObject.name}</b> 위에 있습니다. (UI 타입: {results[0].module.gameObject.name})");
            }
        }

        // 2. 3D/2D 물리 레이캐스트 결과 추적 (건물 콜라이더가 밟히는지 확인)
        Ray ray = _mainCam.ScreenPointToRay(Mouse.current.position.ReadValue());
        if (Physics.Raycast(ray, out RaycastHit hit, 100f))
        {
            Debug.Log($"<color=cyan>[물리 콜라이더 검출]</color> 마우스가 월드의 <b>{hit.collider.gameObject.name}</b> 오브젝트를 조준 중입니다! (레이어: {LayerMask.LayerToName(hit.collider.gameObject.layer)})");
        }
    }
}