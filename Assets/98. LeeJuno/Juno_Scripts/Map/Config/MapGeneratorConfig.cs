using UnityEngine;

/// <summary>
/// 맵 절차적 생성에 필요한 모든 파라미터를 담는 ScriptableObject.
/// 에디터에서 수치를 조정해 맵 난이도와 구성 비율을 튜닝한다.
///
/// 생성 위치: Assets 우클릭 → Create → Bond → MapGeneratorConfig
/// </summary>
[CreateAssetMenu(menuName = "Bond/MapGeneratorConfig")]
public class MapGeneratorConfig : BaseSO
{
    [Header("맵 크기")]
    public int TotalLayers = 15;        // 맵의 총 층 수 (시작층 0 포함)
    public int MinNodesPerLayer = 1;    // 중간 층의 최소 노드 수
    public int MaxNodesPerLayer = 6;    // 중간 층의 최대 노드 수 (열 인덱스 상한 = 이 값 - 1)

    [Header("연결 규칙")]
    public int MinEdgesPerNode = 1;     // 노드 하나가 다음 층으로 최소 몇 개의 경로를 가질지
    public int MaxEdgesPerNode = 3;     // 노드 하나가 다음 층으로 최대 몇 개의 경로를 가질지

    [Header("타입 배치")]
    public int EliteMinLayer = 5;       // Elite 스테이지가 등장할 수 있는 최소 층 번호
    public int MinCampingCount = 2;     // 맵 전체에서 보장할 Camping 노드 최소 개수 (보스 직전층 제외)

    [Header("가중치 (기본값)")]
    // EliteMinLayer 이상인 일반 중간 층에서 사용되는 스테이지 타입 출현 비율.
    // 합계가 1.0이 되도록 설정할 것.
    [Range(0f, 1f)] public float WeightNormal  = 0.51f;
    [Range(0f, 1f)] public float WeightElite   = 0.15f;
    [Range(0f, 1f)] public float WeightEvent   = 0.22f;
    [Range(0f, 1f)] public float WeightCamping = 0.12f;

#if UNITY_EDITOR
    protected override void OnValidate()
    {
        base.OnValidate();
        float sum = WeightNormal + WeightElite + WeightEvent + WeightCamping;
        if (Mathf.Abs(sum - 1f) > 0.01f)
            Debug.LogWarning($"[MapGeneratorConfig] 가중치 합={sum:F2}, 1.0이어야 합니다.", this);
    }
#endif
}
