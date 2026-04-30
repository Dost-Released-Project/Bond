using System;
using BattleSystem.Interface;
using UnityEngine;
using UnityEngine.EventSystems;

namespace BattleSystem
{
    /// <summary>
    /// [L] Logic: 슬롯의 데이터와 상태를 관리합니다.
    /// Logic<CharacterSlotVisualizer>를 상속받아 비주얼과 연결됩니다.
    /// </summary>
    public class CharacterSlot : MonoBehaviour,IPointerEnterHandler, IPointerExitHandler, INeedBind<ICharacterSlotVisualizer>,
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler, ISlot
    {
        [Header("Slot Configuration")] 
        public E_BattleSide side { get; }
        public FormationMask rank { get; }

        public BaseCharacter Occupant { get; private set; }
        public bool IsEmpty => Occupant == null;

        public event Action<CharacterSlot> OnSlotClicked;
        
        private ICharacterSlotVisualizer m_CharacterSlotVisualizer;

        public void SetOccupant(BaseCharacter character)
        { 
            Occupant = character;
            //character.CurrentSlot = this;
        }      
        public void Clear()
        {
            //Occupant.CurrentSlot = null;
            Occupant = null;
        }
        
        public void OnPointerEnter(PointerEventData eventData) => MouseCheck(true, false);
        public void OnPointerExit(PointerEventData eventData) => MouseCheck(false, false);
        public void OnPointerDown(PointerEventData eventData) => MouseCheck(true, true);
        public void OnPointerUp(PointerEventData eventData) => MouseCheck(true, false);
        
        public ColorData colorData;
        
        private Color m_targetColor;
        private Color m_currentColor;
        private bool m_isHovered;
        private bool m_isPressed;

        public CharacterSlot(E_BattleSide side, FormationMask rank)
        {
            this.side = side;
            this.rank = rank;
        }

        private void Start()
        {
            m_currentColor = colorData.normalColor;
            m_targetColor = colorData.normalColor;
            m_CharacterSlotVisualizer.SetBG(colorData.bgColor);
        }

        private void Update()
        {
            VisualUpdate();
        }

        private void MouseCheck(bool isHover, bool isPressed)
        {
            m_isHovered = isHover;
            m_isPressed = isPressed;
        }

        private void VisualUpdate()
        {
            if (m_isPressed) m_targetColor = colorData.pressedColor;
            else if (m_isHovered) m_targetColor = colorData.hoverColor;
            else m_targetColor = colorData.normalColor;

            m_currentColor = Color.Lerp(m_currentColor, m_targetColor, Time.unscaledDeltaTime * colorData.lerpSpeed);
            m_CharacterSlotVisualizer.SetCurrentColor(m_currentColor);
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
