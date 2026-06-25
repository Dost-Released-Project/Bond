using System.Collections.Generic;
using Bond.Expedition;
using UnityEngine;
using VContainer;

/// <summary>
/// 테스트용 고정 맵 생성기.
/// Normal(Layer 0) → Event(Layer 1) → Elite or Camping(Layer 2, 선택) → Boss(Layer 3) 구조를 반환한다.
/// Layer 2에서 Elite / Camping 두 노드 중 하나를 선택할 수 있다.
/// IMapGenerator를 구현하므로 TestMapLifetimeScope의 DI 바인딩만 교체하면
/// MapInitializer 파이프라인 전체가 동일하게 동작한다.
/// </summary>
public class FixedMapGenerator : IMapGenerator
{
    private readonly MapConfigCache _mapConfigCache;
    private readonly ExpeditionPayload _expeditionPayload;

    [Inject]
    public FixedMapGenerator(MapConfigCache mapConfigCache, ExpeditionPayload expeditionPayload)
    {
        _mapConfigCache    = mapConfigCache;
        _expeditionPayload = expeditionPayload;
    }

    public MapData GenerateMap(int seed)
    {
        if (_mapConfigCache.IsReady == false)
        {
            Debug.LogError("[FixedMapGenerator] MapConfigCache 가 아직 준비되지 않았습니다.");
            return null;
        }

        MapData data = new MapData
        {
            Seed = seed,
            TotalLayers = 4,
            MaxNodesPerLayer = 2,
        };

        // Layer 0: Normal — 시작 노드
        MapNode normalNode = new MapNode(0, 0);
        normalNode.StageType = StageType.Normal;
        normalNode.NormalizedPosition = new Vector2(0.5f, 0f);

        // Layer 1: Event
        MapNode eventNode = new MapNode(1, 0);
        eventNode.StageType = StageType.Event;
        eventNode.NormalizedPosition = new Vector2(0.5f, 0.33f);

        // Layer 2: Elite (왼쪽 선택지)
        MapNode eliteNode = new MapNode(2, 0);
        eliteNode.StageType = StageType.Elite;
        eliteNode.NormalizedPosition = new Vector2(0.3f, 0.66f);

        // Layer 2: Camping (오른쪽 선택지)
        MapNode campingNode = new MapNode(2, 1);
        campingNode.StageType = StageType.Camping;
        campingNode.NormalizedPosition = new Vector2(0.7f, 0.66f);

        // Layer 3: Boss — 마지막 노드
        MapNode bossNode = new MapNode(3, 0);
        bossNode.StageType = StageType.Boss;
        bossNode.NormalizedPosition = new Vector2(0.5f, 1f);

        // 연결: Normal → Event → (Elite | Camping) → Boss
        normalNode.NextNodeIds.Add(eventNode.Id);
        eventNode.PrevNodeIds.Add(normalNode.Id);

        eventNode.NextNodeIds.Add(eliteNode.Id);
        eliteNode.PrevNodeIds.Add(eventNode.Id);

        eventNode.NextNodeIds.Add(campingNode.Id);
        campingNode.PrevNodeIds.Add(eventNode.Id);

        eliteNode.NextNodeIds.Add(bossNode.Id);
        bossNode.PrevNodeIds.Add(eliteNode.Id);

        campingNode.NextNodeIds.Add(bossNode.Id);
        bossNode.PrevNodeIds.Add(campingNode.Id);

        data.Nodes.Add(normalNode);
        data.Nodes.Add(eventNode);
        data.Nodes.Add(eliteNode);
        data.Nodes.Add(campingNode);
        data.Nodes.Add(bossNode);

        // NodeById, NodesByLayer 딕셔너리 구성
        data.BuildLookups();

        // 초기 상태: 0층만 Available, 나머지 Locked
        foreach (MapNode node in data.Nodes)
            node.State = NodeState.Locked;

        normalNode.State = NodeState.Available;

        // Normal 노드에 몬스터 그룹 배정
        AssignMonsterGroup(normalNode, seed);

        // Event 노드에 이벤트 배정
        AssignEvent(eventNode, seed);

        // Elite 노드에 엘리트 그룹 배정
        AssignEliteGroup(eliteNode, seed + 1);

        // Boss 노드에 보스 그룹 배정
        AssignBossGroup(bossNode, seed + 2);

        return data;
    }

    /// <summary>
    /// Normal 노드에 IsElite == false 인 몬스터 그룹을 랜덤 배정한다.
    /// MapGenerator 의 AssignMonsterGroups 와 동일한 로직을 사용한다.
    /// </summary>
    private void AssignMonsterGroup(MapNode node, int seed)
    {
        MonsterGroupConfig monsterGroupConfig = _mapConfigCache.MonsterGroupConfig;

        if (monsterGroupConfig == null)
            return;

        if (monsterGroupConfig.Groups == null)
            return;

        if (monsterGroupConfig.Groups.Count == 0)
            return;

        List<MonsterGroupData> candidates = new List<MonsterGroupData>();

        DungeonType currentDungeon = _expeditionPayload?.DungeonType ?? DungeonType.None;

        foreach (MonsterGroupData group in monsterGroupConfig.Groups)
        {
            if (group == null)
                continue;

            // 비엘리트 그룹만 후보로 추가
            if (group.IsElite)
                continue;

            // DungeonType.None / All 이면 모든 던전에 등장, 특정 타입이면 현재 던전과 일치해야 등장
            bool dungeonMatch = group.DungeonType == DungeonType.None
                             || group.DungeonType == DungeonType.All
                             || group.DungeonType == currentDungeon;
            if (dungeonMatch == false)
                continue;

            candidates.Add(group);
        }

        if (candidates.Count == 0)
            return;

        System.Random rng = new System.Random(seed);
        node.AssignedMonsterGroupId = candidates[rng.Next(candidates.Count)].Id;
    }

    private void AssignEliteGroup(MapNode node, int seed)
    {
        MonsterGroupConfig monsterGroupConfig = _mapConfigCache.MonsterGroupConfig;

        if (monsterGroupConfig == null || monsterGroupConfig.Groups == null || monsterGroupConfig.Groups.Count == 0)
            return;

        List<MonsterGroupData> candidates = new List<MonsterGroupData>();

        DungeonType currentDungeon = _expeditionPayload?.DungeonType ?? DungeonType.None;

        foreach (MonsterGroupData group in monsterGroupConfig.Groups)
        {
            if (group == null)
                continue;

            if (group.IsElite == false)
                continue;

            bool dungeonMatch = group.DungeonType == DungeonType.None
                             || group.DungeonType == DungeonType.All
                             || group.DungeonType == currentDungeon;
            if (dungeonMatch == false)
                continue;

            candidates.Add(group);
        }

        if (candidates.Count == 0)
            return;

        System.Random rng = new System.Random(seed);
        node.AssignedMonsterGroupId = candidates[rng.Next(candidates.Count)].Id;
    }

    private void AssignBossGroup(MapNode node, int seed)
    {
        MonsterGroupConfig bossGroupConfig = _mapConfigCache.BossMonsterGroupConfig;

        if (bossGroupConfig == null || bossGroupConfig.Groups == null || bossGroupConfig.Groups.Count == 0)
            return;

        List<MonsterGroupData> candidates = new List<MonsterGroupData>();

        DungeonType currentDungeon = _expeditionPayload?.DungeonType ?? DungeonType.None;

        foreach (MonsterGroupData group in bossGroupConfig.Groups)
        {
            if (group == null)
                continue;

            bool dungeonMatch = group.DungeonType == DungeonType.None
                             || group.DungeonType == DungeonType.All
                             || group.DungeonType == currentDungeon;
            if (dungeonMatch == false)
                continue;

            candidates.Add(group);
        }

        if (candidates.Count == 0)
            return;

        System.Random rng = new System.Random(seed);
        node.AssignedMonsterGroupId = candidates[rng.Next(candidates.Count)].Id;
    }

    /// <summary>
    /// Event 노드에 EventConfig 목록에서 랜덤으로 EventData 를 배정한다.
    /// MapGenerator 의 AssignEvents 와 동일한 로직을 사용한다.
    /// </summary>
    private void AssignEvent(MapNode node, int seed)
    {
        EventConfig eventConfig = _mapConfigCache.EventConfig;

        if (eventConfig == null)
            return;

        if (eventConfig.Events == null)
            return;

        if (eventConfig.Events.Count == 0)
            return;

        List<EventData> candidates = new List<EventData>();

        foreach (EventData ev in eventConfig.Events)
        {
            if (ev != null)
                candidates.Add(ev);
        }

        if (candidates.Count == 0)
            return;

        // seed + 1 로 몬스터 그룹 배정 rng 와 시퀀스 분리
        System.Random rng = new System.Random(seed + 1);
        node.AssignedEventId = candidates[rng.Next(candidates.Count)].Id;
    }
}
