using BattleSystem;
using BattleSystem.Interface;

/// <summary>
/// BaseCharacter 의 진영 이동 편의 메서드. 자신이 보유한 FormationManager 로 이동을 수행한다.
/// (m_formationManager 는 다른 partial 의 private 필드지만 같은 클래스라 접근 가능)
/// </summary>
public partial class BaseCharacter
{
    /// <summary>자기 진영의 index(0=최전방 ~ 3=최후방) 슬롯으로 이동. 대상 슬롯이 차 있으면 교체(Swap)된다.</summary>
    public void MoveToFormationIndex(int index)
    {
        if (m_formationManager == null || CurrentSlot == null) return;
        m_formationManager.MoveCharacter(this, CurrentSlot.side, index);
    }
}
