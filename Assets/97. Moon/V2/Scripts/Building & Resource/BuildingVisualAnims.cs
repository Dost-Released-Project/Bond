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
        new Color(0.8f, 0.8f, 0.85f, 1f), 
        new Color(0.9f, 0.9f, 0.9f, 1f), 
        new Color(1.0f, 1.0f, 1.0f, 1f)    
    };

    public void OnInitialize(BuildingObject owner)
    {
        _owner = owner;
        _spriteRenderer = GetComponent<SpriteRenderer>();
        _collider = GetComponent<BoxCollider>();
        _mainCam = Camera.main;

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

    private void Update()
    {
        if (_collider == null || _mainCam == null || Mouse.current == null || _owner == null) return;

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

            if (_spriteRenderer != null) _spriteRenderer.color = _originColor * 1.1f; 

            if (_tooltipView == null) _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
            if (_tooltipView != null)
            {
                Vector2 uiToolkitPos = mousePosition;
                uiToolkitPos.y = Screen.height - uiToolkitPos.y; 
                _tooltipView.ShowTooltip(_owner, uiToolkitPos);
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
            if (_spriteRenderer != null) _spriteRenderer.color = _originColor; 
            if (_tooltipView == null) _tooltipView = FindFirstObjectByType<BuildingTooltipView>();
            if (_tooltipView != null) _tooltipView.HideTooltip();
        }
    }
}