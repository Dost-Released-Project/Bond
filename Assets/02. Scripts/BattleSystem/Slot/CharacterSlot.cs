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
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Slot Configuration")] 
        public E_BattleSide side;
        public FormationMask rank;

        public BaseCharacter Occupant { get; private set; }
        public bool IsEmpty => Occupant == null;

        public event Action<CharacterSlot> OnSlotClicked;
        
        private ICharacterSlotVisualizer m_CharacterSlotVisualizer;

        public void SetOccupant(BaseCharacter character)
        { 
            Occupant = character;
            character.CurrentSlot = this;
        }      
        public void Clear()
        {
            if (Occupant != null) Occupant.CurrentSlot = null;
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
        
        public bool IsSelected { get; private set; }
        public bool IsTargetable { get; private set; }
        public bool IsActing { get; private set; }
        public bool IsTargeted { get; private set; }

        public void SetSelected(bool val) => IsSelected = val;
        public void SetTargetable(bool val) => IsTargetable = val;
        public void SetActing(bool val) => IsActing = val;
        public void SetTargeted(bool val) => IsTargeted = val;

        public void ResetAllStates()
        {
            IsSelected = false;
            IsTargetable = false;
            IsActing = false;
            IsTargeted = false;
        }

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
            bool isActor = IsSelected || IsActing;
            bool isTarget = IsTargetable || IsTargeted;

            // [사용자 요구사항 절대 준수]
            // 스킬 대상 (Target) = 클릭 효과 (pressedColor)
            // 현재 턴인 캐릭터 (Actor) = 호버 효과 (hoverColor)
            
            if (isTarget)
            {
                m_targetColor = colorData.pressedColor;
            }
            else if (isActor)
            {
                m_targetColor = m_isPressed ? colorData.pressedColor : colorData.hoverColor;
            }
            // 아무 상태도 아닐 때의 기본 마우스 반응
            else
            {
                if (m_isPressed) m_targetColor = colorData.pressedColor;
                else if (m_isHovered) m_targetColor = colorData.hoverColor;
                else m_targetColor = colorData.normalColor;
            }

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
