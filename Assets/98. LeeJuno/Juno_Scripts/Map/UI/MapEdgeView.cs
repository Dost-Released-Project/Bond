using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵에서 두 노드 사이의 경로(엣지)를 Canvas UI Image로 그리는 컴포넌트.
/// LineRenderer는 Canvas 렌더링 파이프라인에서 동작하지 않으므로,
/// Image를 두 노드 중간점에 배치하고 연결 각도로 회전시켜 선을 표현한다.
/// MapView.DrawEdges()에서 엣지마다 하나씩 Instantiate된다.
/// </summary>
[RequireComponent(typeof(Image))]
public class MapEdgeView : MonoBehaviour
{
    [SerializeField] private float _lineWidth = 4f;

    private Image _image;
    private RectTransform _rectTransform;

    private void Awake()
    {
        _image = GetComponent<Image>();
        _rectTransform = GetComponent<RectTransform>();

        // 양 끝에서 정확히 시작/끝나도록 앵커를 중심으로 고정
        _rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        _rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        _rectTransform.pivot     = new Vector2(0.5f, 0.5f);
    }

    /// <summary>
    /// 두 노드의 정규화 좌표를 받아 두 점을 잇는 선을 Image로 그린다.
    /// Image를 두 점의 중간에 배치하고, 길이를 두 점 사이 거리로, 각도를 연결 방향으로 설정한다.
    /// </summary>
    /// <param name="fromNormalized">시작 노드의 정규화 좌표 (0~1)</param>
    /// <param name="toNormalized">끝 노드의 정규화 좌표 (0~1)</param>
    /// <param name="mapRect">변환 기준이 되는 맵 컨테이너 RectTransform</param>
    /// <param name="isActive">true면 불투명 흰색, false면 반투명 (아직 방문 안 한 경로)</param>
    public void Setup(Vector2 fromNormalized, Vector2 toNormalized, RectTransform mapRect, bool isActive)
    {
        Vector2 fromLocal = NormalizedToLocal(fromNormalized, mapRect);
        Vector2 toLocal   = NormalizedToLocal(toNormalized, mapRect);

        Vector2 diff     = toLocal - fromLocal;
        float   distance = diff.magnitude;
        float   angle    = Mathf.Atan2(diff.y, diff.x) * Mathf.Rad2Deg;

        // 중간점에 배치, 너비 = 두 점 사이 거리, 높이 = 선 두께
        _rectTransform.anchoredPosition = (fromLocal + toLocal) * 0.5f;
        _rectTransform.sizeDelta        = new Vector2(distance, _lineWidth);
        _rectTransform.localEulerAngles = new Vector3(0f, 0f, angle);

        _image.color = isActive ? Color.white : new Color(1f, 1f, 1f, 0.3f);
    }

    /// <summary>
    /// 정규화 좌표(0~1)를 RectTransform의 로컬 좌표로 변환한다.
    /// 컨테이너 중심이 (0, 0)이 되도록 0.5를 빼고 rect 크기를 곱한다.
    /// </summary>
    private Vector2 NormalizedToLocal(Vector2 normalized, RectTransform rect)
    {
        float x = (normalized.x - 0.5f) * rect.rect.width;
        float y = (normalized.y - 0.5f) * rect.rect.height;
        return new Vector2(x, y);
    }
}
