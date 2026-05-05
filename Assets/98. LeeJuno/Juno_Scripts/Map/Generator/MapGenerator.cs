using System.Collections.Generic;
using UnityEngine;
using VContainer;

/// <summary>
/// Slay the Spire 스타일의 절차적 맵 생성기.
/// 9단계 파이프라인으로 DAG(방향 비순환 그래프) 구조의 맵을 생성한다.
///
/// 생성 흐름:
///   1. DetermineNodeCounts  — 층별 노드 수 결정
///   2. CreateNodes          — 노드 생성 및 열 위치 배정
///   3. ConnectNodes         — 경로(엣지) 연결
///   4. RemoveCrossings      — 교차 엣지 제거
///   5. AssignStageTypes     — 스테이지 타입 가중치 배정
///   6. ApplyPlacementRules  — 배치 규칙 강제 적용 (보스·캠핑·엘리트 보장)
///   7. InitializeNodeStates — 초기 노드 상태 설정 (0층만 Available)
///   8. AssignMonsterGroups  — Normal 노드에 몬스터 그룹 랜덤 배정
///   9. AssignEvents         — Event 노드에 이벤트 랜덤 배정
/// </summary>
public class MapGenerator : IMapGenerator
{
    private readonly MapConfigCache _mapConfigCache;

    [Inject]
    public MapGenerator(MapConfigCache mapConfigCache)
    {
        _mapConfigCache = mapConfigCache;
    }

    public MapData GenerateMap(int seed)
    {
        if (_mapConfigCache.IsReady == false)
        {
            Debug.LogError("[MapGenerator] MapConfigCache 가 아직 준비되지 않았습니다. MapInitializer.StartAsync() 완료 후 호출해야 합니다.");
            return null;
        }

        MapGeneratorConfig config = _mapConfigCache.GeneratorConfig;
        MonsterGroupConfig monsterGroupConfig = _mapConfigCache.MonsterGroupConfig;
        EventConfig eventConfig = _mapConfigCache.EventConfig;

        // 시드 고정 난수 — 같은 시드면 항상 동일한 맵 생성
        System.Random rng = new System.Random(seed);

        MapData data = new MapData
        {
            Seed = seed,
            TotalLayers = config.TotalLayers,
            MaxNodesPerLayer = config.MaxNodesPerLayer,
        };

        int[] nodeCounts = DetermineNodeCounts(rng, data.TotalLayers, config);
        CreateNodes(data, nodeCounts, rng);
        ConnectNodes(data, rng, config);
        RemoveCrossings(data);
        AssignStageTypes(data, rng, config);
        ApplyPlacementRules(data, rng, config);
        InitializeNodeStates(data);
        AssignMonsterGroups(data, rng, monsterGroupConfig); // Step 8: Normal/Elite 노드에 몬스터 그룹 랜덤 배정
        AssignEvents(data, rng, eventConfig);               // Step 9: Event 노드에 이벤트 랜덤 배정

        return data;
    }

    // ─────────────────────────────────────────────────────────
    // Step 1: 층별 노드 수 결정
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 각 층에 배치할 노드 수를 결정한다.
    /// 0층(시작)과 마지막층(보스)은 항상 노드 1개로 고정한다.
    /// </summary>
    private int[] DetermineNodeCounts(System.Random rng, int totalLayers, MapGeneratorConfig config)
    {
        int[] counts = new int[totalLayers];

        counts[0] = 1;                  // 시작층: 항상 노드 1개
        counts[totalLayers - 1] = 1;   // 보스층: 항상 노드 1개

        for (int i = 1; i < totalLayers - 1; i++)
            counts[i] = rng.Next(config.MinNodesPerLayer, config.MaxNodesPerLayer + 1);

        return counts;
    }

    // ─────────────────────────────────────────────────────────
    // Step 2: 노드 생성 & 열 위치 배정
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 각 층에 노드를 생성하고 가로 열(Column) 위치를 무작위로 배정한다.
    /// UI 배치용 NormalizedPosition(0~1)도 이 단계에서 계산된다.
    /// </summary>
    private void CreateNodes(MapData data, int[] nodeCounts, System.Random rng)
    {
        int maxCol = data.MaxNodesPerLayer;

        for (int layer = 0; layer < data.TotalLayers; layer++)
        {
            int count = nodeCounts[layer];
            int[] columns = PickColumns(maxCol, count, rng);

            for (int i = 0; i < count; i++)
            {
                int col = columns[i];
                MapNode node = new MapNode(layer, col);

                // 정규화 좌표: 맵 컨테이너 크기와 무관하게 0~1로 표현
                float nx = (maxCol > 1) ? (float)col / (maxCol - 1) : 0.5f;
                float ny = (data.TotalLayers > 1) ? (float)layer / (data.TotalLayers - 1) : 0f;
                node.NormalizedPosition = new Vector2(nx, ny);

                data.Nodes.Add(node);
            }
        }

        // 노드 추가 후 딕셔너리 캐시 구성 (이후 단계에서 NodeById, NodesByLayer 사용)
        data.BuildLookups();
    }

    /// <summary>
    /// 0~(max-1) 범위에서 count개를 중복 없이 무작위로 선택해 오름차순으로 반환한다.
    /// Fisher-Yates 셔플을 사용해 편향 없는 열 배치를 보장한다.
    /// </summary>
    private int[] PickColumns(int max, int count, System.Random rng)
    {
        int[] pool = new int[max];
        for (int i = 0; i < max; i++)
            pool[i] = i;

        // Fisher-Yates 셔플 — 배열 전체를 무작위로 섞음
        for (int i = max - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            (pool[i], pool[j]) = (pool[j], pool[i]);
        }

        int[] selected = new int[count];
        for (int i = 0; i < count; i++)
            selected[i] = pool[i];

        // 오름차순 정렬 — 시각적으로 왼쪽부터 차례로 배치되도록
        System.Array.Sort(selected);
        return selected;
    }

    // ─────────────────────────────────────────────────────────
    // Step 3: 경로 연결 (엣지 생성)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 현재 층의 각 노드를 다음 층 노드와 연결한다.
    /// 연결 후보는 열 간격 ±1 이내의 노드로 제한해 경로가 자연스럽게 흐르도록 한다.
    /// 고립 노드(아무도 연결하지 않은 다음 층 노드)가 생기지 않도록 강제 연결도 수행한다.
    /// </summary>
    private void ConnectNodes(MapData data, System.Random rng, MapGeneratorConfig config)
    {
        for (int layer = 0; layer < data.TotalLayers - 1; layer++)
        {
            List<MapNode> currentLayer = data.NodesByLayer[layer];
            List<MapNode> nextLayer = data.NodesByLayer[layer + 1];

            // 각 현재 노드에서 인접 열 노드로 연결
            foreach (MapNode current in currentLayer)
            {
                List<MapNode> candidates = FindCandidates(current, nextLayer);
                int edgeCount = rng.Next(config.MinEdgesPerNode, Mathf.Min(config.MaxEdgesPerNode, candidates.Count) + 1);

                ShuffleList(candidates, rng);

                for (int e = 0; e < edgeCount; e++)
                    AddEdge(current, candidates[e]);
            }

            // 고립 노드 방지: PrevNodeIds가 비어있는 다음 층 노드는 가장 가까운 열의 노드와 강제 연결
            foreach (MapNode next in nextLayer)
            {
                if (next.PrevNodeIds.Count > 0)
                    continue;

                MapNode closest = FindClosestByColumn(next, currentLayer);
                AddEdge(closest, next);
            }
        }
    }

    /// <summary>
    /// from 노드의 열에서 ±1 범위 내에 있는 targets 목록의 노드를 반환한다.
    /// 인접 후보가 없으면 전체 targets를 후보로 반환한다.
    /// </summary>
    private List<MapNode> FindCandidates(MapNode from, List<MapNode> targets)
    {
        List<MapNode> near = new List<MapNode>();

        foreach (MapNode t in targets)
        {
            if (Mathf.Abs(t.Column - from.Column) <= 1)
                near.Add(t);
        }

        return (near.Count > 0) ? near : new List<MapNode>(targets);
    }

    /// <summary>
    /// target과 열 번호가 가장 가까운 노드를 sources에서 찾아 반환한다.
    /// 고립 노드 강제 연결 시 사용된다.
    /// </summary>
    private MapNode FindClosestByColumn(MapNode target, List<MapNode> sources)
    {
        MapNode closest = sources[0];
        int minDist = Mathf.Abs(closest.Column - target.Column);

        foreach (MapNode src in sources)
        {
            int dist = Mathf.Abs(src.Column - target.Column);
            if (dist < minDist)
            {
                minDist = dist;
                closest = src;
            }
        }

        return closest;
    }

    /// <summary>
    /// from → to 방향의 단방향 엣지를 추가한다.
    /// 중복 연결을 방지하기 위해 Contains 확인 후 추가한다.
    /// </summary>
    private void AddEdge(MapNode from, MapNode to)
    {
        if (from.NextNodeIds.Contains(to.Id) == false)
            from.NextNodeIds.Add(to.Id);

        if (to.PrevNodeIds.Contains(from.Id) == false)
            to.PrevNodeIds.Add(from.Id);
    }

    // ─────────────────────────────────────────────────────────
    // Step 4: 교차 엣지 제거 (swap)
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 같은 층에서 시작하는 두 엣지가 시각적으로 교차하면 목적지를 서로 교환한다.
    /// 교차가 없어질 때까지 반복한다.
    ///
    /// 교차 조건: A.Col < B.Col 이면서 C.Col > D.Col (A→C, B→D 교차)
    /// 해결: A→D, B→C 로 목적지를 스왑
    /// </summary>
    private void RemoveCrossings(MapData data)
    {
        for (int layer = 0; layer < data.TotalLayers - 1; layer++)
        {
            List<MapNode> currentLayer = data.NodesByLayer[layer];
            bool hasCrossing = true;

            while (hasCrossing)
            {
                hasCrossing = false;

                for (int i = 0; i < currentLayer.Count; i++)
                {
                    MapNode nodeA = currentLayer[i];

                    for (int j = i + 1; j < currentLayer.Count; j++)
                    {
                        MapNode nodeB = currentLayer[j];

                        for (int ci = 0; ci < nodeA.NextNodeIds.Count; ci++)
                        {
                            MapNode nodeC = data.NodeById[nodeA.NextNodeIds[ci]];

                            for (int di = 0; di < nodeB.NextNodeIds.Count; di++)
                            {
                                MapNode nodeD = data.NodeById[nodeB.NextNodeIds[di]];

                                if (IsCrossing(nodeA, nodeC, nodeB, nodeD) == false)
                                    continue;

                                // 교차 발견 → A→D, B→C 로 스왑하고 PrevNodeIds 동기화
                                nodeA.NextNodeIds[ci] = nodeD.Id;
                                nodeB.NextNodeIds[di] = nodeC.Id;

                                nodeC.PrevNodeIds.Remove(nodeA.Id);
                                nodeD.PrevNodeIds.Remove(nodeB.Id);
                                nodeD.PrevNodeIds.Add(nodeA.Id);
                                nodeC.PrevNodeIds.Add(nodeB.Id);

                                hasCrossing = true;
                            }
                        }
                    }
                }
            }
        }
    }

    /// <summary>
    /// A→C 엣지와 B→D 엣지가 시각적으로 교차하는지 판별한다.
    /// A.Col < B.Col 인데 C.Col > D.Col 이면 교차.
    /// </summary>
    private bool IsCrossing(MapNode fromA, MapNode toC, MapNode fromB, MapNode toD)
    {
        return (fromA.Column < fromB.Column && toC.Column > toD.Column)
            || (fromA.Column > fromB.Column && toC.Column < toD.Column);
    }

    // ─────────────────────────────────────────────────────────
    // Step 5: 스테이지 타입 할당
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 각 노드에 층 번호에 따른 가중치 테이블로 스테이지 타입을 무작위 배정한다.
    /// 보스층·보스직전층은 이 단계에서 예비 배정하고, Step 6에서 강제 교체된다.
    /// </summary>
    private void AssignStageTypes(MapData data, System.Random rng, MapGeneratorConfig config)
    {
        int lastLayer = data.TotalLayers - 1;
        int preBossLayer = lastLayer - 1;

        foreach (MapNode node in data.Nodes)
        {
            node.StageType = PickType(node.Layer, lastLayer, preBossLayer, rng, config);
        }
    }

    /// <summary>
    /// 층 번호에 따라 스테이지 타입을 결정한다.
    ///   - 보스층: Boss 확정
    ///   - 보스 직전층: Camping 확정
    ///   - 0층: Normal 확정 (첫 노드는 항상 일반 전투)
    ///   - 나머지: 층별 가중치 테이블로 결정
    /// </summary>
    private StageType PickType(int layer, int lastLayer, int preBossLayer, System.Random rng, MapGeneratorConfig config)
    {
        if (layer == lastLayer)
            return StageType.Boss;

        if (layer == preBossLayer)
            return StageType.Camping;

        if (layer == 0)
            return StageType.Normal;

        float[] weights = GetWeights(layer, config);
        return WeightedRandom(weights, rng);
    }

    /// <summary>
    /// 층 번호에 따라 스테이지 타입 가중치 배열을 반환한다.
    /// 배열 순서: [Normal, Elite, Event, Camping]
    /// </summary>
    private float[] GetWeights(int layer, MapGeneratorConfig config)
    {
        if (layer < 4)
            return new float[] { 0.70f, 0f, 0.30f, 0f };

        if (layer < config.EliteMinLayer)
            return new float[] { 0.55f, 0f, 0.25f, 0.15f };

        return new float[] {
            config.WeightNormal,
            config.WeightElite,
            config.WeightEvent,
            config.WeightCamping,
        };
    }

    /// <summary>
    /// 가중치 배열을 기반으로 확률적으로 스테이지 타입 하나를 선택한다.
    /// 가중치 합이 1.0이 아니어도 내부에서 정규화한다.
    /// </summary>
    private StageType WeightedRandom(float[] weights, System.Random rng)
    {
        float total = 0f;
        foreach (float w in weights)
            total += w;

        float roll = (float)rng.NextDouble() * total;
        float cumulative = 0f;

        StageType[] types = { StageType.Normal, StageType.Elite, StageType.Event, StageType.Camping };

        for (int i = 0; i < weights.Length; i++)
        {
            cumulative += weights[i];
            if (roll <= cumulative)
                return types[i];
        }

        return StageType.Normal;
    }

    // ─────────────────────────────────────────────────────────
    // Step 6: 배치 규칙 후처리
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 게임 설계 규칙을 강제 적용한다.
    ///   규칙 1: 보스층 = 전부 Boss
    ///   규칙 2: 보스 직전층 = 전부 Camping (휴식 보장)
    ///   규칙 3: Elite 최소 1개 보장 (EliteMinLayer ~ preBossLayer-1 구간)
    ///   규칙 4: Camping 최소 MinCampingCount개 보장 (보스 직전층 제외한 구간)
    ///   규칙 5: 보스층 외에 Boss 타입이 남아있으면 Normal로 교체
    /// </summary>
    private void ApplyPlacementRules(MapData data, System.Random rng, MapGeneratorConfig config)
    {
        int lastLayer = data.TotalLayers - 1;
        int preBossLayer = lastLayer - 1;

        // 규칙 1: 보스층 강제
        foreach (MapNode node in data.NodesByLayer[lastLayer])
            node.StageType = StageType.Boss;

        // 규칙 2: 보스 직전층 강제
        foreach (MapNode node in data.NodesByLayer[preBossLayer])
            node.StageType = StageType.Camping;

        // 규칙 3 & 4 전처리: EliteMinLayer ~ preBossLayer-1 구간을 단일 순회로 통합
        // HasTypeInRange + FindRandomNormalInRange 를 동일 구간에서 연속 호출하는
        // 중복 순회를 제거하기 위해 구간 내 노드를 한 번만 순회한다.
        int eliteRangeEnd = preBossLayer - 1;
        bool hasElite = false;
        List<MapNode> eliteRangeCandidates = new List<MapNode>(); // Elite 보장용 Normal 후보

        for (int layer = config.EliteMinLayer; layer <= eliteRangeEnd; layer++)
        {
            if (data.NodesByLayer.ContainsKey(layer) == false)
                continue;

            foreach (MapNode node in data.NodesByLayer[layer])
            {
                if (node.StageType == StageType.Elite)
                    hasElite = true;

                if (node.StageType == StageType.Normal)
                    eliteRangeCandidates.Add(node);
            }
        }

        // 규칙 3: Elite 최소 1개 보장
        if (hasElite == false && eliteRangeCandidates.Count > 0)
        {
            MapNode target = eliteRangeCandidates[rng.Next(eliteRangeCandidates.Count)];
            target.StageType = StageType.Elite;
        }

        // 규칙 4: Camping 최소 개수 보장
        int campingCount = CountTypeExcluding(data, StageType.Camping, preBossLayer);
        if (campingCount < config.MinCampingCount)
        {
            int needed = config.MinCampingCount - campingCount;
            for (int i = 0; i < needed; i++)
            {
                MapNode target = FindRandomNormalInRange(data, 4, preBossLayer - 1, rng);
                if (target != null)
                    target.StageType = StageType.Camping;
            }
        }

        // 규칙 5: 보스층 외 Boss 타입 제거
        foreach (MapNode node in data.Nodes)
        {
            if (node.StageType == StageType.Boss && node.Layer < lastLayer)
                node.StageType = StageType.Normal;
        }
    }

    /// <summary>
    /// fromLayer~toLayer 구간의 Normal 노드 중 하나를 무작위로 반환한다.
    /// NodesByLayer를 활용해 해당 층만 접근한다.
    /// 후보가 없으면 null을 반환한다.
    /// </summary>
    private MapNode FindRandomNormalInRange(MapData data, int fromLayer, int toLayer, System.Random rng)
    {
        List<MapNode> candidates = new List<MapNode>();

        for (int layer = fromLayer; layer <= toLayer; layer++)
        {
            if (data.NodesByLayer.ContainsKey(layer) == false)
                continue;

            foreach (MapNode node in data.NodesByLayer[layer])
            {
                if (node.StageType == StageType.Normal)
                    candidates.Add(node);
            }
        }

        if (candidates.Count == 0)
            return null;

        return candidates[rng.Next(candidates.Count)];
    }

    /// <summary>
    /// 특정 층(excludeLayer)을 제외하고 해당 타입의 노드 수를 센다.
    /// Camping 최소 보장 수량 계산 시 보스 직전층을 제외하기 위해 사용한다.
    /// </summary>
    private int CountTypeExcluding(MapData data, StageType type, int excludeLayer)
    {
        int count = 0;

        foreach (MapNode node in data.Nodes)
        {
            if (node.StageType == type && node.Layer != excludeLayer)
                count++;
        }

        return count;
    }

    // ─────────────────────────────────────────────────────────
    // Step 7: 초기 노드 상태 설정
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// 전체 노드를 Locked으로 초기화한 뒤, 0층 노드만 Available로 설정한다.
    /// 플레이어는 0층 노드 중 하나를 선택해 여정을 시작한다.
    /// </summary>
    private void InitializeNodeStates(MapData data)
    {
        foreach (MapNode node in data.Nodes)
            node.State = NodeState.Locked;

        if (data.NodesByLayer.ContainsKey(0) == false)
            return;

        foreach (MapNode node in data.NodesByLayer[0])
            node.State = NodeState.Available;
    }

    // ─────────────────────────────────────────────────────────
    // 유틸
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Fisher-Yates 알고리즘으로 리스트를 제자리에서 무작위로 섞는다.
    /// </summary>
    private void ShuffleList(List<MapNode> list, System.Random rng)
    {
        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            MapNode tmp = list[i];
            list[i] = list[j];
            list[j] = tmp;
        }
    }

    // ─────────────────────────────────────────────────────────
    // Step 8: Normal/Elite 노드 몬스터 그룹 배정
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Normal 타입 노드에는 IsElite == false 그룹을, Elite 타입 노드에는 IsElite == true 그룹을 배정한다.
    /// rng 는 파이프라인 전체에서 공유되므로 Step 7 완료 후 호출해야
    /// 같은 seed 에서 항상 동일한 결과가 보장된다.
    /// </summary>
    private void AssignMonsterGroups(MapData data, System.Random rng, MonsterGroupConfig monsterGroupConfig)
    {
        if (monsterGroupConfig == null)
            return;

        if (monsterGroupConfig.Groups == null)
            return;

        if (monsterGroupConfig.Groups.Count == 0)
            return;

        foreach (MapNode node in data.Nodes)
        {
            bool isElite;

            if (node.StageType == StageType.Normal)
                isElite = false;
            else if (node.StageType == StageType.Elite)
                isElite = true;
            else
                continue;

            List<MonsterGroupData> candidates = GetCandidateGroups(node.Layer, isElite, monsterGroupConfig);

            if (candidates.Count == 0)
                continue;

            node.AssignedMonsterGroupId = candidates[rng.Next(candidates.Count)].Id;
        }
    }

    /// <summary>
    /// 지정 층 번호와 등급에 맞는 몬스터 그룹 목록을 반환한다.
    /// MinLayer == 0 &amp;&amp; MaxLayer == 0 이면 층 제한 없음으로 처리한다.
    /// isElite: true이면 IsElite == true 그룹만, false이면 IsElite == false 그룹만 반환한다.
    /// </summary>
    private List<MonsterGroupData> GetCandidateGroups(int layer, bool isElite, MonsterGroupConfig monsterGroupConfig)
    {
        List<MonsterGroupData> candidates = new List<MonsterGroupData>();

        foreach (MonsterGroupData group in monsterGroupConfig.Groups)
        {
            // Inspector에서 리스트에 빈 슬롯이 있을 경우 NullReferenceException 방지
            if (group == null)
                continue;

            if (group.IsElite != isElite)
                continue;

            bool noLayerLimit = group.MinLayer == 0 && group.MaxLayer == 0;
            bool inRange = layer >= group.MinLayer && layer <= group.MaxLayer;

            if (noLayerLimit || inRange)
                candidates.Add(group);
        }

        return candidates;
    }

    // ─────────────────────────────────────────────────────────
    // Step 9: Event 노드 이벤트 배정
    // ─────────────────────────────────────────────────────────

    /// <summary>
    /// Event 타입 노드에 EventConfig 목록에서 랜덤으로 EventData 를 배정한다.
    /// rng 는 파이프라인 전체에서 공유되므로 Step 8 완료 후 호출해야
    /// 같은 seed 에서 항상 동일한 결과가 보장된다.
    /// </summary>
    private void AssignEvents(MapData data, System.Random rng, EventConfig eventConfig)
    {
        if (eventConfig == null)
            return;

        if (eventConfig.Events == null)
            return;

        if (eventConfig.Events.Count == 0)
            return;

        List<EventData> candidates = GetCandidateEvents(eventConfig); // 루프 밖에서 한 번만 호출한다

        if (candidates.Count == 0)
            return;

        foreach (MapNode node in data.Nodes)
        {
            if (node.StageType != StageType.Event)
                continue;

            node.AssignedEventId = candidates[rng.Next(candidates.Count)].Id;
        }
    }

    /// <summary>
    /// 배정 가능한 이벤트 목록을 반환한다.
    /// 현재 EventData 에 층 범위 필드가 없으므로 전체 목록을 반환한다.
    /// 추후 EventData 에 MinLayer/MaxLayer 가 추가되면 이 메서드에 범위 필터를 추가한다.
    /// </summary>
    private List<EventData> GetCandidateEvents(EventConfig eventConfig)
    {
        List<EventData> candidates = new List<EventData>();

        foreach (EventData ev in eventConfig.Events)
        {
            // Inspector 에서 리스트에 빈 슬롯이 있을 경우 NullReferenceException 방지
            if (ev == null)
                continue;

            candidates.Add(ev);
        }

        return candidates;
    }
}
