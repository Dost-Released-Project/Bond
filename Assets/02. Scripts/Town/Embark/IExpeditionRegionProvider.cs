using System.Collections.Generic;

namespace Bond.Expedition
{
    /// <summary>
    /// 선택 가능한 탐사 지역 목록을 제공한다. 추후 실제 데이터(DBSO) 기반 구현으로 교체 예정.
    /// </summary>
    public interface IExpeditionRegionProvider
    {
        IReadOnlyList<ExpeditionRegion> GetRegions();
    }
}
