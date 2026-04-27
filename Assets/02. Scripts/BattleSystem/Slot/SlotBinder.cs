using _02._Scripts.BattleSystem;
using UnityEngine;

public class SlotBinder : BaseBinder<ICharacterSlotVisualizer, CharacterSlot>
{
    private CharacterSlot m_characterSlot;
    private ICharacterSlotVisualizer m_visualizer;

    private void Awake()
    {
        m_characterSlot = GetComponent<CharacterSlot>();
        m_visualizer = GetComponent<ICharacterSlotVisualizer>();

        if (m_characterSlot != null && m_visualizer != null)
        {
            Bind(m_visualizer, m_characterSlot);
        }
        else
        {
            Debug.LogWarning($"[SlotBinder] {gameObject.name}에 필요한 Logic 또는 Visual 컴포넌트가 없습니다.");
        }
    }
}
