using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 맵 노드의 선택 가능 여부를 나타내는 상태값.
/// MapNavigator.MoveToNode() 호출 시 상태가 전환된다.
/// </summary>
public enum NodeState
{
    Locked,     // 선택 불가 (아직 도달할 수 없는 노드)
    Available,  // 선택 가능 (현재 노드의 다음 층 노드들)
    Visited,    // 방문 완료 (이미 지나온 노드)
    Current,    // 현재 위치 (플레이어가 있는 노드)
}

/// <summary>
/// 맵을 구성하는 단일 노드의 데이터.
/// JSON 직렬화 대상이므로 [System.Serializable] 적용.
/// Id = Layer * 100 + Column 규칙으로 고유성 보장 (Column은 최대 99까지).
/// </summary>
[System.Serializable]
public class MapNode
{
    public int Id;                       // 고유 ID = Layer * 100 + Column
    public int Layer;                    // 세로 층 번호 (0 = 시작층, TotalLayers-1 = 보스층)
    public int Column;                   // 가로 열 인덱스 (0 ~ MaxNodesPerLayer-1)

    public StageType StageType;          // 이 노드에서 진행될 스테이지 종류
    public NodeState State;              // 현재 선택 가능 여부 상태

    public List<int> NextNodeIds;        // 다음 층(Layer+1) 연결 노드 ID 목록
    public List<int> PrevNodeIds;        // 이전 층(Layer-1) 연결 노드 ID 목록

    public Vector2 NormalizedPosition;   // UI 배치용 정규화 좌표 (x, y 모두 0~1 범위)

    /// <summary>
    /// 층과 열 번호로 노드를 초기화한다. Id는 자동 계산된다.
    /// </summary>
    public MapNode(int layer, int column)
    {
        Layer = layer;
        Column = column;
        Id = layer * 100 + column;
        State = NodeState.Locked;
        StageType = StageType.Normal;
        NextNodeIds = new List<int>();
        PrevNodeIds = new List<int>();
    }
}
