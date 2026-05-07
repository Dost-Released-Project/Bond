using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 전체를 Canvas UI로 렌더링하는 컴포넌트.
/// Initialize() 호출 시 노드(MapNodeView)와 엣지(MapEdgeView)를 Instantiate해 배치한다.
///
/// Inspector 연결 필요:
///   _mapContainer  — 노드·엣지가 배치될 부모 RectTransform
///   _nodeViewPrefab— MapNodeView가 붙은 프리팹
///   _edgeViewPrefab— MapEdgeView + LineRenderer가 붙은 프리팹
///   _stageConfigs  — StageType별 아이콘·색상 설정 목록
/// </summary>
public class MapView : MonoBehaviour
{
    [SerializeField] private RectTransform _mapContainer;
    [SerializeField] private MapNodeView _nodeViewPrefab;
    [SerializeField] private MapEdgeView _edgeViewPrefab;
    [SerializeField] private List<StageConfig> _stageConfigs;

    private MapData _mapData;
    private Dictionary<int, MapNodeView> _nodeViews;                          // NodeId → MapNodeView 빠른 접근용
    private List<(int FromNodeId, MapEdgeView EdgeView)> _edgeViews;          // 엣지 캐싱 (출발 노드 ID + 뷰)
    private Dictionary<StageType, StageConfig> _stageConfigDict;              // StageType → StageConfig O(1) 조회용
    private System.Action<int> _onNodeClickedCallback;
    private ISpriteLoader _spriteLoader;

    /// <summary>
    /// 맵 데이터를 받아 노드와 엣지를 생성하고 배치한다.
    /// 이미 그려진 상태에서 재호출하면 기존 오브젝트 위에 중복 생성되므로 주의.
    /// </summary>
    /// <param name="mapData">렌더링할 맵 데이터</param>
    /// <param name="onNodeClicked">노드 버튼 클릭 시 호출할 콜백 (인자: 노드 Id)</param>
    /// <param name="spriteLoader">MapNodeView 에 전달할 Addressables Sprite 로드 서비스.</param>
    public void Initialize(MapData mapData, System.Action<int> onNodeClicked, ISpriteLoader spriteLoader)
    {
        _spriteLoader = spriteLoader;
        foreach (Transform child in _mapContainer)
            UnityEngine.Object.Destroy(child.gameObject);

        _mapData = mapData;
        _onNodeClickedCallback = onNodeClicked;
        _nodeViews = new Dictionary<int, MapNodeView>();
        _edgeViews = new List<(int, MapEdgeView)>();

        // StageConfig 딕셔너리를 한 번만 구성해 FindConfig() 의 O(n) 순회를 O(1) 조회로 대체한다.
        _stageConfigDict = new Dictionary<StageType, StageConfig>();
        if (_stageConfigs != null)
        {
            foreach (StageConfig cfg in _stageConfigs)
            {
                // Inspector 빈 슬롯에 의한 null 요소 방지
                if (cfg == null)
                    continue;

                _stageConfigDict[cfg.Type] = cfg;
            }
        }

        DrawEdges();
        DrawNodes();
    }

    /// <summary>
    /// 모든 노드뷰의 상태(버튼 활성화, 강조)를 현재 MapNode.State에 맞게 갱신한다.
    /// 엣지뷰의 색상도 함께 갱신해 방문한 경로를 시각적으로 반영한다.
    /// MapUIController.OnNodeEntered 후 호출된다.
    /// </summary>
    public void RefreshNodeStates()
    {
        if (_nodeViews == null || _edgeViews == null)
            return;

        foreach (KeyValuePair<int, MapNodeView> pair in _nodeViews)
            pair.Value.RefreshState();

        // 엣지 상태 갱신: 출발 노드가 Visited 또는 Current이면 활성(불투명) 표시
        foreach ((int fromNodeId, MapEdgeView edgeView) in _edgeViews)
        {
            if (_mapData.NodeById.ContainsKey(fromNodeId) == false)
                continue;

            MapNode fromNode = _mapData.NodeById[fromNodeId];
            bool isActive = fromNode.State == NodeState.Visited || fromNode.State == NodeState.Current;
            edgeView.RefreshState(isActive);
        }
    }

    /// <summary>
    /// 모든 노드의 NextNodeIds를 순회해 엣지를 생성한다.
    /// 방문한 노드(Visited/Current)에서 나온 엣지는 불투명, 나머지는 반투명으로 표시한다.
    /// </summary>
    private void DrawEdges()
    {
        foreach (MapNode node in _mapData.Nodes)
        {
            foreach (int nextId in node.NextNodeIds)
            {
                if (_mapData.NodeById.ContainsKey(nextId) == false)
                    continue;

                MapNode nextNode = _mapData.NodeById[nextId];
                MapEdgeView edgeView = Instantiate(_edgeViewPrefab, _mapContainer);

                bool isActive = node.State == NodeState.Visited || node.State == NodeState.Current;
                edgeView.Setup(node.NormalizedPosition, nextNode.NormalizedPosition, _mapContainer, isActive);

                // 엣지 캐싱: 출발 노드 ID와 뷰를 함께 저장해 RefreshNodeStates에서 갱신 가능하게 한다
                _edgeViews.Add((node.Id, edgeView));
            }
        }
    }

    /// <summary>
    /// 모든 노드를 순회해 MapNodeView를 생성하고 정규화 좌표로 위치를 지정한다.
    /// StageConfig에서 아이콘 주소·fallback 스프라이트·색상을 가져와 노드뷰를 초기화한다.
    /// 아이콘 실제 로드는 MapNodeView 내부에서 비동기로 처리된다.
    /// </summary>
    private void DrawNodes()
    {
        if (_stageConfigs == null)
        {
            Debug.LogError("[MapView] _stageConfigs가 연결되지 않았습니다.", this);
            return;
        }

        foreach (MapNode node in _mapData.Nodes)
        {
            MapNodeView nodeView = Instantiate(_nodeViewPrefab, _mapContainer);
            RectTransform rt = nodeView.GetComponent<RectTransform>();
            rt.anchoredPosition = NormalizedToAnchored(node.NormalizedPosition);

            StageConfig config = FindConfig(node.StageType);
            string iconAddress = (config != null) ? config.IconAddress : string.Empty;
            Sprite fallbackIcon = (config != null) ? config.Icon : null;
            Color color = (config != null) ? config.NodeColor : Color.white;

            nodeView.Setup(node, iconAddress, fallbackIcon, _onNodeClickedCallback, _spriteLoader);
            nodeView.SetColor(color);

            _nodeViews[node.Id] = nodeView;
        }
    }

    /// <summary>
    /// 정규화 좌표(0~1)를 _mapContainer 기준 앵커드 포지션으로 변환한다.
    /// 컨테이너 중심 (0,0)을 기준으로 ±width/2, ±height/2 범위로 변환한다.
    /// </summary>
    private Vector2 NormalizedToAnchored(Vector2 normalized)
    {
        float x = (normalized.x - 0.5f) * _mapContainer.rect.width;
        float y = (normalized.y - 0.5f) * _mapContainer.rect.height;
        return new Vector2(x, y);
    }

    /// <summary>
    /// StageType에 대응하는 StageConfig를 딕셔너리에서 O(1) 조회로 반환한다.
    /// Initialize() 에서 _stageConfigDict 가 구성된 이후에 호출되어야 한다.
    /// 해당하는 Config가 없으면 null을 반환한다.
    /// </summary>
    private StageConfig FindConfig(StageType stageType)
    {
        if (_stageConfigDict == null)
            return null;

        StageConfig result;
        _stageConfigDict.TryGetValue(stageType, out result);
        return result;
    }
}
