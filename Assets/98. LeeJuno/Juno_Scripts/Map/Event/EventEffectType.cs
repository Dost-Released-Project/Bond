/// <summary>
/// 이벤트 선택지가 적용하는 효과의 종류.
/// EventEffectData.EffectType 에 사용된다.
/// </summary>
public enum EffectType
{
    None,           // 효과 없음
    HpChange,       // HP 증감 (SingleCharacter / Party 둘 다 사용)
    StatusEffect,   // 버프·디버프·상태이상 (미구현 시스템 연동 예정)
    ItemReward,     // 아이템 획득
    Battle,         // 전투 씬 전환
}

/// <summary>
/// 효과를 적용할 대상 선택 방식.
/// EffectType 이 HpChange·StatusEffect 일 때 사용한다.
/// </summary>
public enum TargetType
{
    None,           // 대상 없음 (ItemReward, Battle 등에서 사용)
    ChooseOne,      // 플레이어가 파티 목록 UI 에서 직접 1명 선택
    RandomOne,      // 파티 중 랜덤 1명에게 자동 적용
    All,            // 파티 전원에게 적용
}

/// <summary>
/// 아이템 획득 방식.
/// EffectType 이 ItemReward 일 때 사용한다.
/// </summary>
public enum ItemRewardType
{
    Guaranteed,         // 지정 아이템 무조건 획득
    Probability,        // 지정 확률로 획득 (실패 시 아무것도 없음)
    RandomFromPool,     // 아이템 풀에서 랜덤 1개 획득
}
