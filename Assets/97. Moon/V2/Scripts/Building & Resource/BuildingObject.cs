using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingObject : MonoBehaviour
{
    public BuildingData Data { get; private set; }
    public int CurrentLevel { get; private set; } = 1;

    private ISettlementManager _manager;
    private SpriteRenderer _spriteRenderer;
    
    private BuildingTooltipView _tooltipView;
    private bool _isHovered = false; 
    private Camera _mainCam;

    private Color _originColor;

    [Header("레벨별 건물 색상")]
    [SerializeField] private Color[] _levelColors = new Color[] 
    {
        new Color(0.8f, 0.8f, 0.8f, 1f), 
        new Color(0.9f, 0.9f, 0.9f, 1f), 
        new Color(1.0f, 1.0f, 1.0f, 1f)    
    };

    public void Initialize(BuildingData data, ISettlementManager manager)
    {
        Data = data;
        _manager = manager;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _mainCam = Camera.main;
        
        _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
        RefreshBuildingVisual();
    }

    // 인게임 유저 인터랙션 전용 업그레이드 (두근 연출 포함)
    public void Upgrade()
    {
        if (Data != null && CurrentLevel < Data.levels.Count)
        {
            CurrentLevel++;
            RefreshBuildingVisual();
            TriggerConstructionPopping(); // 💥 여기는 인게임 클릭용이므로 연출 보존!
        }
    }

    // =========================================================================
    // 💾 [신규 추가] 세이브 로딩 전용 강제 레벨 셋업 기능 (두근 연출 0%)
    // =========================================================================
    public void LoadLevelForce(int targetLevel)
    {
        CurrentLevel = targetLevel;
        
        // 연출 함수를 완전히 생략하고 쥐죽은 듯 데이터와 스케일 틴트만 매핑합니다.
        RefreshBuildingVisual();
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

    public void TriggerConstructionPopping()
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(CoPoppingRoutine());
    }

    private System.Collections.IEnumerator CoPoppingRoutine()
    {
        Vector3 baseScale = GetBaseScaleByLevel(CurrentLevel);
        Vector3 peakScale = baseScale * 1.25f; 

        float elapsed = 0f;
        while (elapsed < 0.06f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(baseScale, peakScale, elapsed / 0.06f);
            yield return null;
        }

        elapsed = 0f;
        while (elapsed < 0.14f)
        {
            elapsed += Time.deltaTime;
            transform.localScale = Vector3.Lerp(peakScale, baseScale, elapsed / 0.14f);
            yield return null;
        }
        
        transform.localScale = baseScale;
    }

    private void Update()
    {
        if (_mainCam == null || Mouse.current == null) return;

        if (UnityEngine.EventSystems.EventSystem.current != null && 
            UnityEngine.EventSystems.EventSystem.current.IsPointerOverGameObject())
        {
            ResetHoverState();
            return;
        }

        Vector2 mousePosition = Mouse.current.position.ReadValue();
        Ray ray = _mainCam.ScreenPointToRay(mousePosition);
        
        if (Physics.Raycast(ray, out RaycastHit hit, 100f) && hit.collider.gameObject == this.gameObject)
        {
            if (_isHovered) return; 

            _isHovered = true;

            if (_spriteRenderer != null)
            {
                _spriteRenderer.color = _originColor * 1.1f; 
            }

            if (_tooltipView == null) _tooltipView = FindFirstObjectByType<BuildingTooltipView>();

            if (_tooltipView != null)
            {
                Vector2 uiToolkitPos = mousePosition;
                uiToolkitPos.y = Screen.height - uiToolkitPos.y; 
                _tooltipView.ShowTooltip(this, uiToolkitPos);
            }
        }
        else
        {
            ResetHoverState();
        }
    }

    private void ResetHoverState()
    {
        if (_isHovered)
        {
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