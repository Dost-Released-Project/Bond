using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingObject : MonoBehaviour
{
    public BuildingData Data { get; private set; }
    public int CurrentLevel { get; private set; } = 1;

    private ISettlementManager _manager;
    private SpriteRenderer _spriteRenderer;
    private BoxCollider _collider;
    
    private BuildingTooltipView _tooltipView;
    private bool _isHovered = false; // 💥 마우스 진입/이탈 상태를 제어하는 핵심 플래그
    private Camera _mainCam;

    private Color _originColor;

    [Header("레벨별 건물 색상")]
    [SerializeField] private Color[] _levelColors = new Color[] 
    {
        new Color(0.8f, 0.8f, 0.85f, 1f), 
        new Color(0.9f, 0.9f, 0.9f, 1f), 
        new Color(1.0f, 1.0f, 1.0f, 1f)    
    };

    public void Initialize(BuildingData data, ISettlementManager manager)
    {
        Data = data;
        _manager = manager;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<BoxCollider>();
        _mainCam = Camera.main;

        if (_collider != null)
        {
            _collider.size = new Vector3(_collider.size.x, _collider.size.y, 2.0f);
        }
        
        _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
        RefreshBuildingVisual();
    }

    public void Upgrade()
    {
        if (Data != null && CurrentLevel < Data.levels.Count)
        {
            CurrentLevel++;
            RefreshBuildingVisual();
        }
    }

    private void RefreshBuildingVisual()
    {
        Vector3 targetBaseScale = GetBaseScaleByLevel(CurrentLevel);
        transform.localScale = targetBaseScale;

        if (_spriteRenderer != null && _levelColors != null && _levelColors.Length > 0)
        {
            int colorIndex = CurrentLevel - 1;
            if (colorIndex >= _levelColors.Length) colorIndex = _levelColors.Length - 1;

            _spriteRenderer.color = _levelColors[colorIndex];
            _originColor = _levelColors[colorIndex]; 
        }
    }

    private Vector3 GetBaseScaleByLevel(int level)
    {
        float scaleMultiplier = 0.6f + (level - 1) * 0.1f;
        return new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
    }

    // =========================================================================
    // 🎯 [최종 교정] 무한 난사 부하 0% 및 UI 관통 완벽 차단 스마트 레이어
    // =========================================================================
    private void Update()
    {
        if (_mainCam == null || Mouse.current == null) return;

        // 💥 [방어막 1] 다른 인벤토리창이나 UI가 마우스 앞을 가로막고 있다면, 
        // 뒤에 숨은 건물의 모든 업데이트 물리 연산을 즉시 중단하고 이탈 처리합니다.
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            ResetHoverState();
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCam.ScreenPointToRay(mousePosition);
        
        // 유니티 전역 물리 공간에서 내 박스 콜라이더를 밟았는지 판정
        if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.collider.gameObject == this.gameObject)
        {
            // 💥 [방어막 2 - 리더님 제안 기획 구현] 
            // 이미 마우스가 올라와 있는 상태(_isHovered == true)라면 하위 연산을 전부 패스합니다! (로그 난사 차단)
            if (_isHovered) return; 

            // 오직 마우스가 건물 경계선 안으로 들어온 '첫 프레임에 딱 1번만' 아래 코드가 실행됩니다.
            _isHovered = true;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originColor * 1.1f; 
                Debug.Log($"<color=yellow>[하이라이트 1회 호출 완료]</color> {Data.DisplayName} 색상 변경 처리.");
            }

            // 💥 널 체크 예방 및 실시간 동적 캐싱 보완
            if (_tooltipView == null) _tooltipView = FindFirstObjectByType<BuildingTooltipView>();

            if (_tooltipView != null)
            {
                Vector2 uiToolkitPos = mousePosition;
                uiToolkitPos.y = Screen.height - uiToolkitPos.y; 
                
                Debug.Log($"<color=orange>[툴팁 1회 호출 완료]</color> {Data.DisplayName} 툴팁 표출 성공.");
                _tooltipView.ShowTooltip(this, uiToolkitPos);
            }
        }
        else
        {
            // 마우스가 내 건물 영역을 완전히 벗어났을 때 복원
            ResetHoverState();
        }
    }

    // 이탈 및 UI 가려짐 시 비주얼을 단 1번만 초기화하는 서브 루틴
    private void ResetHoverState()
    {
        if (_isHovered)
        {
            Debug.Log($"<color=magenta>[마우스 이탈 1회 처리]</color> {Data.DisplayName}에서 완전히 벗어남.");
            _isHovered = false;
            
            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originColor; 
            }
            
            if (_tooltipView == null) _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
            if (_tooltipView != null) _tooltipView.HideTooltip();
        }
    }

    private void OnMouseDown()
    {
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (_manager != null)
        {
            _manager.OnBuildingClicked(this);
        }
    }
}