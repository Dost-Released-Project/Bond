using System;
using UnityEngine;
using UnityEngine.EventSystems;

namespace _02._Scripts.BattleSystem
{
    /// <summary>
    /// [L] Logic: 슬롯의 데이터와 상태를 관리합니다.
    /// Logic<CharacterSlotVisualizer>를 상속받아 비주얼과 연결됩니다.
    /// </summary>
    public class CharacterSlot : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler, INeedBind<ICharacterSlotVisualizer>,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Slot Configuration")]
        public E_BattleSide side;
        public FormationMask rank;

        public BaseCharacter Occupant { get; private set; }
        public bool IsEmpty => Occupant == null;

        public event Action<CharacterSlot> OnSlotClicked;
        
        private ICharacterSlotVisualizer m_CharacterSlotVisualizer;

        public void SetOccupant(BaseCharacter character) => Occupant = character;       
        public void Clear() => Occupant = null;
        
        public void OnPointerEnter(PointerEventData eventData) => MouseCheck(true, false);
        public void OnPointerExit(PointerEventData eventData) => MouseCheck(false, false);
        public void OnPointerDown(PointerEventData eventData) => MouseCheck(true, true);
        public void OnPointerUp(PointerEventData eventData) => MouseCheck(true, false);
        
        public ColorData colorData;
        
        private Color m_TargetColor;
        private Color m_CurrentColor;
        private bool m_IsHovered;
        private bool m_IsPressed;

        private void Start()
        {
            m_CurrentColor = colorData.normalColor;
            m_TargetColor = colorData.normalColor;
            m_CharacterSlotVisualizer.SetBG(colorData.bgColor);
        }

        private void Update()
        {
            VisualUpdate();
        }

        private void MouseCheck(bool isHover, bool isPressed)
        {
            m_IsHovered = isHover;
            m_IsPressed = isPressed;
        }

        private void VisualUpdate()
        {
            if (m_IsPressed) m_TargetColor = colorData.pressedColor;
            else if (m_IsHovered) m_TargetColor = colorData.hoverColor;
            else m_TargetColor = colorData.normalColor;

            m_CurrentColor = Color.Lerp(m_CurrentColor, m_TargetColor, Time.unscaledDeltaTime * colorData.lerpSpeed);
            m_CharacterSlotVisualizer.SetCurrentColor(m_CurrentColor);
        }
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log("Button Clicked");
            OnSlotClicked?.Invoke(this);
        }

        public void Bind(ICharacterSlotVisualizer targetClass)
        {
            m_CharacterSlotVisualizer = targetClass;
        }
    }
}
