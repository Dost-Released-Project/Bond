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
    private bool _isHovered = false;
    private Camera _mainCam;

    private Color _originColor;

    public void Initialize(BuildingData data, ISettlementManager manager)
    {
        Data = data;
        _manager = manager;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<BoxCollider>();
        _mainCam = Camera.main;

        if (_spriteRenderer != null)
        {
            _originColor = _spriteRenderer.color; // 건물의 원래 색상 보존
        }
        
        _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
    }

    // 💥 업그레이드가 순차적으로 정상 반영되도록 레벨 상한값 안전장치 추가
    public void Upgrade()
    {
        if (Data != null && CurrentLevel < Data.levels.Count)
        {
            CurrentLevel++;
        }
    }

    private void Update()
    {
        if (_collider == null || _mainCam == null || Mouse.current == null) return;

        // 최적화: 마우스 정지 상태 및 비호버 시 물리 연산 완전 패스
        if (Mouse.current.delta.ReadValue() == Vector2.zero && !_isHovered) return;

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCam.ScreenPointToRay(mousePosition);
        
        if (_collider.Raycast(ray, out RaycastHit hit, 100f))
        {
            // 1. [마우스 진입] 사각형으로 깨지는 아웃라인 대신 건물이 은은하게 화사해지는 발광(Glow) 효과 적용
            if (!_isHovered)
            {
                _isHovered = true;
                if (_spriteRenderer != null)
                {
                    // RGB 값을 1 이상으로 살짝 올려 단일 스프라이트 자체를 화사하게 빛나게 만듭니다.
                    _spriteRenderer.color = new Color(1.3f, 1.3f, 1.5f, 1.0f); 
                }
            }

            // 2. [마우스 오버] 툴팁 출력 및 실시간 좌표 보정
            if (_tooltipView != null)
            {
                Vector2 uiToolkitPos = mousePosition;
                uiToolkitPos.y = Screen.height - uiToolkitPos.y; 
                _tooltipView.ShowTooltip(this, uiToolkitPos);
            }
        }
        else
        {
            // 4. [마우스 이탈] 완전 원복
            if (_isHovered)
            {
                _isHovered = false;
                
                if (_spriteRenderer != null)
                {
                    _spriteRenderer.color = _originColor; // 원래 색상으로 복원
                }
                
                if (_tooltipView != null)
                    _tooltipView.HideTooltip();
            }
        }
    }

    // =========================================================================
    // 🖱️ 리더님 요청 반영: 기존에 완벽하게 잘 작동하던 순수 마우스 클릭 로직으로 복구
    // =========================================================================
    private void OnMouseDown()
    {
        // UI가 마우스를 막고 있을 때는 클릭 무시
        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject()) return;

        if (_manager != null)
        {
            _manager.OnBuildingClicked(this);
        }
    }
}