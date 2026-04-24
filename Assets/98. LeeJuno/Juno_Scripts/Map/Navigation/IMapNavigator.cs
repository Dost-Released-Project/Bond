using System;
using System.Collections.Generic;

/// <summary>
/// 맵 이동 및 노드 상태 관리 인터페이스.
/// 구현체(MapNavigator)는 VContainer를 통해 주입된다.
/// </summary>
public interface IMapNavigator
{
    /// <summary>
    /// 플레이어가 새 노드에 진입했을 때 발생하는 이벤트.
    /// MapUIController가 구독해 스테이지 씬 로드를 시작한다.
    /// </summary>
    public event Action<MapNode> OnNodeEntered;

    /// <summary>현재 플레이어가 위치한 노드. 시작 전에는 null.</summary>
    public MapNode CurrentNode { get; }

    /// <summary>
    /// 맵 데이터를 받아 내비게이터를 초기화한다.
    /// 저장된 게임을 불러올 때도 이 메서드를 호출해 CurrentNode를 복원한다.
    /// </summary>
    public void Initialize(MapData mapData);

    /// <summary>
    /// 지정한 노드 ID로 이동을 시도한다.
    /// Available 상태인 노드에만 이동 가능하며, 성공 시 OnNodeEntered 이벤트를 발생시킨다.
    /// </summary>
    /// <returns>이동 성공 여부 (Available 노드가 아니면 false)</returns>
    public bool MoveToNode(int nodeId);

    /// <summary>
    /// 현재 선택 가능한(Available) 노드 목록을 반환한다.
    /// </summary>
    public List<MapNode> GetAvailableNodes();
}
