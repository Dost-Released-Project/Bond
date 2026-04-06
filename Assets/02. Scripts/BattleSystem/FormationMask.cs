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
        Form1   = 1 << 0, // 최전방 (Front)
        Form2   = 1 << 1,
        Form3   = 1 << 2,
        Form4   = 1 << 3, // 최후방 (Back)
        
        // 자주 사용되는 프리셋
        FrontTwo = Form1 | Form2,
        BackTwo  = Form3 | Form4,
        Any      = Form1 | Form2 | Form3 | Form4
    }
}
