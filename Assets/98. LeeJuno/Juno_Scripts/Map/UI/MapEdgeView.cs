using UnityEngine;

/// <summary>
/// 맵에서 두 노드 사이의 경로(엣지)를 LineRenderer로 그리는 컴포넌트.
/// MapView.DrawEdges()에서 엣지마다 하나씩 Instantiate된다.
/// useWorldSpace = false로 설정해 Canvas 로컬 좌표계에서 동작한다.
/// </summary>
[RequireComponent(typeof(LineRenderer))]
public class MapEdgeView : MonoBehaviour
{
    private LineRenderer _lineRenderer;

    private void Awake()
    {
        _lineRenderer = GetComponent<LineRenderer>();
        _lineRenderer.positionCount = 2;        // 시작점과 끝점 2개
        _lineRenderer.useWorldSpace = false;    // Canvas 로컬 좌표 사용
    }

    /// <summary>
    /// 두 노드의 정규화 좌표를 받아 Canvas 로컬 좌표로 변환한 뒤 선을 그린다.
    /// </summary>
    /// <param name="fromNormalized">시작 노드의 정규화 좌표 (0~1)</param>
    /// <param name="toNormalized">끝 노드의 정규화 좌표 (0~1)</param>
    /// <param name="mapRect">변환 기준이 되는 맵 컨테이너 RectTransform</param>
    /// <param name="isActive">true면 불투명 흰색, false면 반투명 (아직 방문 안 한 경로)</param>
    public void Setup(Vector2 fromNormalized, Vector2 toNormalized, RectTransform mapRect, bool isActive)
    {
        Vector2 fromLocal = NormalizedToLocal(fromNormalized, mapRect);
        Vector2 toLocal = NormalizedToLocal(toNormalized, mapRect);

        _lineRenderer.SetPosition(0, new Vector3(fromLocal.x, fromLocal.y, 0f));
        _lineRenderer.SetPosition(1, new Vector3(toLocal.x, toLocal.y, 0f));

        // 방문한 경로는 불투명, 아직 지나지 않은 경로는 반투명으로 표시
        Color lineColor = isActive ? Color.white : new Color(1f, 1f, 1f, 0.3f);
        _lineRenderer.startColor = lineColor;
        _lineRenderer.endColor = lineColor;
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
