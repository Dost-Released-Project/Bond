/// <summary>
/// 스킬 시트 타입 코드 (ID 3~4번째 자리) 에 대응하는 열거형
/// 01=OFFENSIVE, 02=DEFENSIVE, 03=SPELL, 04=SUPPORT
/// </summary>
public enum SkillType
{
    OFFENSIVE = 1,  // 01 - 공격형
    DEFENSIVE = 2,  // 02 - 방어형
    SPELL = 3,      // 03 - 주문형
    SUPPORT = 4     // 04 - 보조형
}

/// <summary>
/// 스킬 타겟 범위 열거형 (시트 Target 컬럼)
/// </summary>
public enum SkillTarget
{
    Enemy,  // 적 대상
    Party,  // 아군 대상
    Self    // 자기 자신 (확장용)
}
