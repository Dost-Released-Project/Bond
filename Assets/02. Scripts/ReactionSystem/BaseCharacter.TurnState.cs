/// <summary>
/// BaseCharacter 의 턴 스킵 상태. SkipTurnReactionEffect 가 설정하고
/// TurnManager 가 자기 턴 시작(버프/봉인 tick 직후)에 소비한다.
/// </summary>
public partial class BaseCharacter
{
    private int _skipTurns;

    /// <summary>다음 count 번의 자기 턴을 행동 불가로 만든다.</summary>
    public void RequestSkipTurns(int count)
    {
        if (count > 0) _skipTurns += count;
    }

    /// <summary>이번 자기 턴을 건너뛸지. 건너뛰면 카운트 1을 소비하고 true 를 반환.</summary>
    public bool ConsumeSkipTurn()
    {
        if (_skipTurns <= 0) return false;
        _skipTurns--;
        return true;
    }
}
