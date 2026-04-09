using System;

namespace _02._Scripts.BattleSystem
{
    /// <summary>
    /// 다키스트 던전 스타일의 진영 위치(Rank)를 비트마스크로 정의합니다.
    /// </summary>
    [Flags]
    public enum FormationMask
    {
        None    = 0,
        Rank1   = 1 << 0, // 최전방 (Front)
        Rank2   = 1 << 1,
        Rank3   = 1 << 2,
        Rank4   = 1 << 3, // 최후방 (Back)
        
        // 자주 사용되는 프리셋
        FrontLine = Rank1 | Rank2,
        BackLine  = Rank3 | Rank4,
        Middle    = Rank2 | Rank3,
        NotFront  = Rank2 | Rank3 | Rank4,
        NotBack   = Rank1 | Rank2 | Rank3,
        Any       = Rank1 | Rank2 | Rank3 | Rank4
    }
}
