using UnityEngine;
using UnityEngine.UI;

/// <summary>선 방향. 쉐이더의 _LineDirection 값에 대응한다.</summary>
public enum SpeedLineDirection
{
    Horizontal = 0, // 가로선
    Vertical   = 1  // 세로선
}

/// <summary>
/// RawImage에 SpeedLineUI 머티리얼을 할당하고 쉐이더 파라미터를 Inspector 및 코드에서 제어한다.
/// ExecuteAlways로 Edit 모드에서도 Inspector 변경이 즉시 반영된다.
/// </summary>
[ExecuteAlways]
public class SpeedLineUIController : MonoBehaviour
{
    /// <summary>스피드라인을 표시할 RawImage. Inspector에서 연결한다.</summary>
    [SerializeField] private RawImage _rawImage;

    [Header("Speed Line Parameters")]

    /// <summary>스피드라인 색상.</summary>
    [SerializeField] private Color _color = Color.white;

    /// <summary>선 방향. Horizontal = 가로선, Vertical = 세로선.</summary>
    [SerializeField] private SpeedLineDirection _lineDirection = SpeedLineDirection.Horizontal;

    /// <summary>선 굵기 (UV 공간 기준 0~0.5).</summary>
    [SerializeField] [Range(0f, 0.5f)] private float _lineThickness = 0.02f;

    /// <summary>선 간격 (UV 반복 주기). 값이 작을수록 선이 촘촘하다.</summary>
    [SerializeField] [Range(0.01f, 1f)] private float _lineSpacing = 0.1f;

    [Header("Scroll")]

    /// <summary>X축 스크롤 속도 (초당 UV 이동량). 양수 = 오른쪽.</summary>
    [SerializeField] private float _scrollX = 0f;

    /// <summary>Y축 스크롤 속도 (초당 UV 이동량). 양수 = 위쪽.</summary>
    [SerializeField] private float _scrollY = 1f;

    [Header("Display")]

    /// <summary>전체 알파. 페이드인/아웃 제어에 사용한다.</summary>
    [SerializeField] [Range(0f, 1f)] private float _alpha = 1f;

    /// <summary>선 경계 블렌딩 폭. 0이면 하드엣지.</summary>
    [SerializeField] [Range(0f, 0.5f)] private float _edgeSoftness = 0.005f;

    // 쉐이더 프로퍼티 ID 캐싱 (GetPropertyID는 한 번만 호출하는 게 효율적이다)
    private static readonly int s_ColorId          = Shader.PropertyToID("_Color");
    private static readonly int s_UnscaledTimeId   = Shader.PropertyToID("_UnscaledTime");
    private static readonly int s_LineDirectionId  = Shader.PropertyToID("_LineDirection");
    private static readonly int s_LineThicknessId  = Shader.PropertyToID("_LineThickness");
    private static readonly int s_LineSpacingId    = Shader.PropertyToID("_LineSpacing");
    private static readonly int s_ScrollXId        = Shader.PropertyToID("_ScrollX");
    private static readonly int s_ScrollYId        = Shader.PropertyToID("_ScrollY");
    private static readonly int s_AlphaId          = Shader.PropertyToID("_Alpha");
    private static readonly int s_EdgeSoftnessId   = Shader.PropertyToID("_EdgeSoftness");

    private Material _materialInstance;

    /// <summary>현재 전체 알파값. 외부에서 페이드 제어 시 사용한다.</summary>
    public float Alpha
    {
        get => _alpha;
        set
        {
            _alpha = Mathf.Clamp01(value);
            ApplyAlpha();
        }
    }

    /// <summary>X축 스크롤 속도. 외부에서 연출 제어 시 사용한다.</summary>
    public float ScrollX
    {
        get => _scrollX;
        set
        {
            _scrollX = value;
            // 단일 파라미터 변경이므로 람다 대신 직접 호출한다
            if (_materialInstance != null)
            {
                _materialInstance.SetFloat(s_ScrollXId, _scrollX);
            }
        }
    }

    /// <summary>Y축 스크롤 속도. 외부에서 연출 제어 시 사용한다.</summary>
    public float ScrollY
    {
        get => _scrollY;
        set
        {
            _scrollY = value;
            if (_materialInstance != null)
            {
                _materialInstance.SetFloat(s_ScrollYId, _scrollY);
            }
        }
    }

    private void Awake()
    {
        InitializeMaterialInstance();
    }

    private void Update()
    {
        if (_materialInstance == null)
        {
            return;
        }

        _materialInstance.SetFloat(s_UnscaledTimeId, Time.unscaledTime);
    }

    private void OnValidate()
    {
        // Edit 모드에서 Inspector 값 변경 시 즉시 반영한다
        if (_materialInstance == null)
        {
            return;
        }

        ApplyAllProperties();
    }

    private void OnDestroy()
    {
        // 인스턴스화된 머티리얼은 수동으로 해제해야 메모리 누수가 없다
        if (_materialInstance != null)
        {
            Destroy(_materialInstance);
        }
    }

    /// <summary>
    /// 스피드라인 효과를 즉시 활성화한다. RawImage가 활성화되며 파라미터를 전부 적용한다.
    /// </summary>
    public void Show()
    {
        if (_rawImage == null)
        {
            return;
        }

        _rawImage.gameObject.SetActive(true);
        ApplyAllProperties();
    }

    /// <summary>
    /// 스피드라인 효과를 즉시 비활성화한다.
    /// </summary>
    public void Hide()
    {
        if (_rawImage == null)
        {
            return;
        }

        _rawImage.gameObject.SetActive(false);
    }

    /// <summary>
    /// 모든 쉐이더 파라미터를 Material Instance에 일괄 적용한다.
    /// </summary>
    public void ApplyAllProperties()
    {
        if (_materialInstance == null)
        {
            return;
        }

        _materialInstance.SetColor(s_ColorId,          _color);
        _materialInstance.SetFloat(s_LineDirectionId,  (float)_lineDirection);
        _materialInstance.SetFloat(s_LineThicknessId,  _lineThickness);
        _materialInstance.SetFloat(s_LineSpacingId,    _lineSpacing);
        _materialInstance.SetFloat(s_ScrollXId,        _scrollX);
        _materialInstance.SetFloat(s_ScrollYId,        _scrollY);
        _materialInstance.SetFloat(s_AlphaId,          _alpha);
        _materialInstance.SetFloat(s_EdgeSoftnessId,   _edgeSoftness);
    }

    private void InitializeMaterialInstance()
    {
        if (_rawImage == null)
        {
            Debug.LogWarning("[SpeedLineUIController] RawImage가 연결되지 않았습니다.");
            return;
        }

        if (_rawImage.material == null)
        {
            Debug.LogWarning("[SpeedLineUIController] RawImage에 머티리얼이 없습니다. Inspector에서 SpeedLineUI 머티리얼을 연결하세요.");
            return;
        }

        // 공유 머티리얼 수정 방지를 위해 인스턴스를 복사한다
        _materialInstance          = new Material(_rawImage.material);
        _rawImage.material         = _materialInstance;

        ApplyAllProperties();
    }

    private void ApplyAlpha()
    {
        if (_materialInstance == null)
        {
            return;
        }

        _materialInstance.SetFloat(s_AlphaId, _alpha);
    }

}
