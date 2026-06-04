/// <summary>
/// BaseCharacter 의 연쇄(리액션 발동) 카운터 파트.
/// ReactionCall 이 리액션 발동마다 IncrementReactionCount 를 호출하고,
/// TurnManager 가 자기 턴 도달 시(돌발 판정 이후) ResetReactionCount 로 초기화한다.
/// 따라서 "연속 N회" = 마지막 자기 턴 이후 발동한 리액션 수.
/// </summary>
public partial class BaseCharacter
{
    private int _reactionsFired;

    /// <summary>마지막 자기 턴 이후 이 캐릭터가 발동한 리액션 수.</summary>
    public int ReactionsFiredCount => _reactionsFired;

    public void IncrementReactionCount() => _reactionsFired++;

    public void ResetReactionCount() => _reactionsFired = 0;
}
