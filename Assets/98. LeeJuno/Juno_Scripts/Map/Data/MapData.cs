using System;
using System.Collections.Generic;

/// <summary>
/// 한 챕터(Act)의 맵 전체 데이터 컨테이너.
/// JSON으로 저장/불러오기 가능하도록 [Serializable] 적용.
///
/// 주의: Dictionary 필드는 JsonUtility가 직렬화하지 못하므로 [NonSerialized] 처리.
///       파일에서 불러온 뒤 반드시 BuildLookups()를 호출해 딕셔너리를 재구성해야 한다.
/// </summary>
[Serializable]
public class MapData
{
    public int Seed;                // 맵 생성에 사용된 시드값 — 같은 시드면 동일한 맵이 생성됨
    public int ActNumber;           // 현재 챕터 번호 (1부터 시작)
    public int TotalLayers;         // 맵의 총 층 수
    public int MaxNodesPerLayer;    // 층당 최대 노드 수 (열 인덱스 상한)

    public List<MapNode> Nodes;     // 맵의 모든 노드 목록 (직렬화됨)
    public int CurrentNodeId;       // 현재 플레이어 위치 노드 ID (-1이면 아직 시작 전)

    // 빠른 조회를 위한 캐시 딕셔너리 — 직렬화 불가, BuildLookups()로 재구성
    [NonSerialized] public Dictionary<int, MapNode> NodeById;
    [NonSerialized] public Dictionary<int, List<MapNode>> NodesByLayer;

    public MapData()
    {
        Nodes = new List<MapNode>();
        CurrentNodeId = -1;
    }

    /// <summary>
    /// Nodes 목록을 기반으로 NodeById와 NodesByLayer 딕셔너리를 구성한다.
    /// 노드 생성 직후(CreateNodes 끝) 또는 JSON 역직렬화 직후 반드시 호출해야 한다.
    /// </summary>
    public void BuildLookups()
    {
        NodeById = new Dictionary<int, MapNode>();
        NodesByLayer = new Dictionary<int, List<MapNode>>();

        foreach (MapNode node in Nodes)
        {
            NodeById[node.Id] = node;

            if (NodesByLayer.ContainsKey(node.Layer) == false)
                NodesByLayer[node.Layer] = new List<MapNode>();

            NodesByLayer[node.Layer].Add(node);
        }
    }
}
