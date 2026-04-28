using System;
using System.Collections.Generic;
using VContainer;

/// <summary>
/// IMapNavigator 구현체.
/// 플레이어가 맵 노드를 이동할 때 상태 전환을 처리하고, 맵 데이터를 저장한다.
///
/// 이동 시 상태 전환 규칙:
///   이전 CurrentNode → Visited
///   이전 Current의 NextNodeIds → Locked (선택하지 않은 경로는 막힘, Slay the Spire 방식)
///   선택한 노드 → Current
///   선택한 노드의 NextNodeIds → Available (다음 층 선택지 오픈)
/// </summary>
public class MapNavigator : IMapNavigator
{
    private readonly IMapRepository _repository;

    private MapData _mapData;

    [Inject]
    public MapNavigator(IMapRepository repository)
    {
        _repository = repository;
    }

    /// <summary>
    /// 플레이어가 노드에 진입할 때 발생. MapUIController가 구독해 스테이지 씬을 로드한다.
    /// </summary>
    public event Action<MapNode> OnNodeEntered;

    /// <summary>현재 플레이어가 위치한 노드. Initialize() 호출 전 또는 시작 전이면 null.</summary>
    public MapNode CurrentNode { get; private set; }

    /// <summary>
    /// 맵 데이터를 받아 초기화한다. 저장된 게임 불러오기 시에도 호출해 CurrentNode를 복원한다.
    /// </summary>
    public void Initialize(MapData mapData)
    {
        _mapData = mapData;
        CurrentNode = null;

        if (_mapData.NodeById == null)
            _mapData.BuildLookups();

        // 저장된 CurrentNodeId가 있으면 해당 노드를 복원
        if (_mapData.CurrentNodeId == -1)
            return;

        if (_mapData.NodeById.ContainsKey(_mapData.CurrentNodeId))
            CurrentNode = _mapData.NodeById[_mapData.CurrentNodeId];
    }

    /// <summary>
    /// 지정한 nodeId의 노드로 이동을 시도한다.
    /// Available 상태인 노드에만 이동 가능하다.
    /// 성공 시 상태를 전환하고, 맵을 저장한 뒤 OnNodeEntered를 발생시킨다.
    /// </summary>
    public bool MoveToNode(int nodeId)
    {
        if (_mapData == null)
            return false;

        if (_mapData.NodeById.ContainsKey(nodeId) == false)
            return false;

        MapNode target = _mapData.NodeById[nodeId];

        // Available 상태가 아니면 이동 불가
        if (target.State != NodeState.Available)
            return false;

        // 이전 노드 정리: Visited 처리 + 선택하지 않은 다음 노드 잠금
        if (CurrentNode != null)
        {
            CurrentNode.State = NodeState.Visited;

            // Slay the Spire 방식: 같은 층의 다른 경로는 영구 잠금
            foreach (int nextId in CurrentNode.NextNodeIds)
            {
                if (_mapData.NodeById.ContainsKey(nextId))
                    _mapData.NodeById[nextId].State = NodeState.Locked;
            }
        }

        // 새 노드를 Current로 설정
        target.State = NodeState.Current;
        CurrentNode = target;
        _mapData.CurrentNodeId = nodeId;

        // 다음 층 노드 Available 처리
        UnlockNextNodes(target);

        // 변경된 맵 상태 저장
        _repository.Save(_mapData);

        OnNodeEntered?.Invoke(target);
        return true;
    }

    /// <summary>
    /// 현재 Available 상태인 모든 노드를 반환한다.
    /// </summary>
    public List<MapNode> GetAvailableNodes()
    {
        List<MapNode> available = new List<MapNode>();

        if (_mapData == null)
            return available;

        foreach (MapNode node in _mapData.Nodes)
        {
            if (node.State == NodeState.Available)
                available.Add(node);
        }

        return available;
    }

    /// <summary>
    /// 방금 진입한 노드의 NextNodeIds를 Available로 전환한다.
    /// 이미 Locked가 아닌 노드는 건드리지 않는다.
    /// </summary>
    private void UnlockNextNodes(MapNode node)
    {
        foreach (int nextId in node.NextNodeIds)
        {
            if (_mapData.NodeById.ContainsKey(nextId) == false)
                continue;

            MapNode next = _mapData.NodeById[nextId];

            if (next.State == NodeState.Locked)
                next.State = NodeState.Available;
        }
    }
}
