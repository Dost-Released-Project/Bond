using System.Collections.Generic;

namespace Bond.Expedition
{
    /// <summary>
    /// 기능 확인용 임시 프로바이더. 하드코딩된 탐사 지역 목록을 반환한다.
    /// 추후 실제 데이터 기반 구현으로 교체된다.
    /// </summary>
    public class TestExpeditionRegionProvider : IExpeditionRegionProvider
    {
        private readonly List<ExpeditionRegion> _regions = new()
        {
            new ExpeditionRegion("forest_01", "속삭이는 숲", "난이도: 평이 · 권장 4인", DungeonType.Forest),
            new ExpeditionRegion("ruin_01",   "무너진 성채", "난이도: 위험 · 권장 4인", DungeonType.Ruin),
        };

        public IReadOnlyList<ExpeditionRegion> GetRegions() => _regions;
    }
}
