using UnityEngine;
using UnityEngine.InputSystem;

public class BuildingVisualAnims : MonoBehaviour
{
    private BuildingObject _owner;
    private SpriteRenderer _spriteRenderer;
    private BoxCollider _collider;
    private BuildingTooltipView _tooltipView;
    private Camera _mainCam;
    
    private bool _isHovered = false;
    private Color _originColor;
    

    [Header("레벨별 건물 색상")]
    [SerializeField] private Color[] _levelColors = new Color[] 
    {
        new Color(0.8f, 0.8f, 0.8f, 1f), 
        new Color(0.9f, 0.9f, 0.9f, 1f), 
        new Color(1.0f, 1.0f, 1.0f, 1f)    
    };
    
    public void OnInitialize(BuildingObject owner)
    {
        _owner = owner;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _tooltipView = FindFirstObjectByType<BuildingTooltipView>();

        RefreshVisual(_owner.CurrentLevel);
    }

    public void RefreshVisual(int level)
    {
        float scaleMultiplier = 0.6f + (level - 1) * 0.1f;
        transform.localScale = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);

        if (_spriteRenderer != null && _levelColors != null && _levelColors.Length > 0)
        {
            int colorIndex = level - 1;
            if (colorIndex >= _levelColors.Length) colorIndex = _levelColors.Length - 1;

            _spriteRenderer.color = _levelColors[colorIndex];
            _originColor = _levelColors[colorIndex]; 
        }
    }

    // =========================================================================
    // 🎯 [중앙 집중화 전환] Update()를 삭제하고 호출당하는 인터페이스로 격상
    // =========================================================================
    public void TriggerHoverState(Vector2 mousePosition)
    {
        if (_isHovered) return;
        _isHovered = true;

        if (_spriteRenderer != null) _spriteRenderer.color = _originColor * 1.1f; 

        if (_tooltipView == null) _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
        if (_tooltipView != null && _owner != null)
        {
            Vector2 uiToolkitPos = mousePosition;
            uiToolkitPos.y = Screen.height - uiToolkitPos.y; 
            _tooltipView.ShowTooltip(_owner, uiToolkitPos);
        }
    }

    public void ResetHoverState()
    {
        if (!_isHovered) return;
        _isHovered = false;

        if (_spriteRenderer != null) _spriteRenderer.color = _originColor; 
        if (_tooltipView == null) _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
        if (_tooltipView != null) _tooltipView.HideTooltip();
    }

    // 외부 외부 충격(업그레이드 등) 시 단발성 갱신 기능 슬롯 유지
    public void ForceRefreshTooltip()
    {
        if (!_isHovered || _owner == null) return;

        if (_tooltipView == null) _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
        if (_tooltipView != null)
        {
            Vector2 mousePosition = Mouse.current.position.ReadValue();
            Vector2 uiToolkitPos = mousePosition;
            uiToolkitPos.y = Screen.height - uiToolkitPos.y;
            _tooltipView.ShowTooltip(_owner, uiToolkitPos);
        }
    }

    // 건설 이펙트(팝핑)
    public void TriggerConstructionPopping(int level)
    {
        if (!gameObject.activeInHierarchy) return;
        StopAllCoroutines();
        StartCoroutine(CoPoppingRoutine(level));
    }

    private System.Collections.IEnumerator CoPoppingRoutine(int level)
    {
        float scaleMultiplier = 0.6f + (level - 1) * 0.1f;
        Vector3 baseScale = new Vector3(scaleMultiplier, scaleMultiplier, scaleMultiplier);
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
}