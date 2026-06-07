namespace Bond.Expedition
{
    /// <summary>
    /// 탐사 지역 1개의 데이터. 추후 DBSO(ScriptableObject) 기반으로 교체 예정인 임시 순수 C# 모델.
    /// DungeonType 을 보유하여 맵 시스템이 기존처럼 던전 타입을 읽을 수 있게 한다.
    /// </summary>
    public class ExpeditionRegion
    {
        public string Id { get; }
        public string DisplayName { get; }
        public string Meta { get; }
        public DungeonType DungeonType { get; }

        public ExpeditionRegion(string id, string displayName, string meta, DungeonType dungeonType)
        {
            Id = id;
            DisplayName = displayName;
            Meta = meta;
            DungeonType = dungeonType;
        }
    }
}
