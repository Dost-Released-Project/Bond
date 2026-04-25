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
    private Dictionary<int, MapNodeView> _nodeViews; // NodeId → MapNodeView 빠른 접근용
    private System.Action<int> _onNodeClickedCallback;

    /// <summary>
    /// 맵 데이터를 받아 노드와 엣지를 생성하고 배치한다.
    /// 이미 그려진 상태에서 재호출하면 기존 오브젝트 위에 중복 생성되므로 주의.
    /// </summary>
    /// <param name="mapData">렌더링할 맵 데이터</param>
    /// <param name="onNodeClicked">노드 버튼 클릭 시 호출할 콜백 (인자: 노드 Id)</param>
    public void Initialize(MapData mapData, System.Action<int> onNodeClicked)
    {
        foreach (Transform child in _mapContainer)
            UnityEngine.Object.Destroy(child.gameObject);

        _mapData = mapData;
        _onNodeClickedCallback = onNodeClicked;
        _nodeViews = new Dictionary<int, MapNodeView>();

        DrawEdges();
        DrawNodes();
    }

    /// <summary>
    /// 모든 노드뷰의 상태(버튼 활성화, 강조)를 현재 MapNode.State에 맞게 갱신한다.
    /// MapUIController.OnNodeEntered 후 호출된다.
    /// </summary>
    public void RefreshNodeStates()
    {
        foreach (KeyValuePair<int, MapNodeView> pair in _nodeViews)
            pair.Value.RefreshState();
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
            }
        }
    }

    /// <summary>
    /// 모든 노드를 순회해 MapNodeView를 생성하고 정규화 좌표로 위치를 지정한다.
    /// StageConfig에서 아이콘과 색상을 가져와 노드뷰를 초기화한다.
    /// </summary>
    private void DrawNodes()
    {
        foreach (MapNode node in _mapData.Nodes)
        {
            MapNodeView nodeView = Instantiate(_nodeViewPrefab, _mapContainer);
            nodeView.GetComponent<RectTransform>().anchoredPosition = NormalizedToAnchored(node.NormalizedPosition);

            StageConfig config = FindConfig(node.StageType);
            Sprite icon = (config != null) ? config.Icon : null;
            Color color = (config != null) ? config.NodeColor : Color.white;

            nodeView.Setup(node, icon, _onNodeClickedCallback);
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
    /// StageType에 대응하는 StageConfig를 목록에서 찾아 반환한다.
    /// 해당하는 Config가 없으면 null을 반환한다.
    /// </summary>
    private StageConfig FindConfig(StageType stageType)
    {
        foreach (StageConfig config in _stageConfigs)
        {
            if (config.Type == stageType)
                return config;
        }

        return null;
    }
}
