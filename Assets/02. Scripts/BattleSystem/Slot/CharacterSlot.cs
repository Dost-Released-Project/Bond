using System;
using BattleSystem.Interface;
using BattleSystem.UI;
using Cysharp.Threading.Tasks;
using Shapes;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;

namespace BattleSystem
{
    public enum SlotImageType
    {
        Idle,
        Attack
    }

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

        [Header("Visual Effects")]
        [SerializeField] private TextVisualizer m_TextVisualizer;
        [SerializeField] private CharacterSlotBar m_CharacterSlotBar;
        [SerializeField] private CharacterSlotBuffBar m_CharacterSlotBuffBar;

        public BaseCharacter Occupant { get; private set; }
        public bool IsEmpty => Occupant == null;

        public event Action<CharacterSlot> OnSlotClicked;
        
        private ICharacterSlotVisualizer m_CharacterSlotVisualizer;
        private SlotImageType m_currentImageType = SlotImageType.Idle;

        public void SetOccupant(BaseCharacter character)
        { 
            Occupant = character;
            character.CurrentSlot = this;

            character.OnDamageTaken += ShowDamageText;
            character.OnHealed += ShowHealText;
            character.OnEvaded += ShowEvadedText;
            character.OnHpChanged += UpdateHpBar;
            character.OnInsanityChanged += UpdateInsanityBar;
            character.OnBuffsChanged += UpdateBuffs;

            // 초기 UI 갱신
            UpdateHpBar(character);
            UpdateInsanityBar(character);
            UpdateBuffs(character);
            
            // 기본 이미지 타입(Idle)으로 로드
            m_currentImageType = SlotImageType.Idle;
            UpdatePortraitAsync(character).Forget();
        }

        public void SetImageType(SlotImageType type)
        {
            if (m_currentImageType == type) return;
            m_currentImageType = type;
            UpdatePortraitAsync(Occupant).Forget();
        }

        private async UniTask UpdatePortraitAsync(BaseCharacter character)
        {
            if (character == null) return;

            // 1. 선택된 타입에 따른 캐시 및 주소 결정
            Texture cachedSprite = m_currentImageType switch
            {
                SlotImageType.Idle => character.IdlePortrait,
                SlotImageType.Attack => character.AttackPortrait,
                _ => null
            };

            string address = m_currentImageType switch
            {
                SlotImageType.Idle => character.EffectiveIdleImageAddress,
                SlotImageType.Attack => character.EffectiveAttackImageAddress,
                _ => null
            };

            // 2. 이미 캐싱된 스프라이트가 있다면 즉시 적용
            if (cachedSprite != null)
            {
                m_CharacterSlotVisualizer.SetPortrait(cachedSprite);
                return;
            }

            // 3. 캐시가 없고 주소가 유효하다면 Addressables 비동기 로드
            if (!string.IsNullOrEmpty(address))
            {
                try
                {
                    // Sprite가 아닌 Texture로 로드하여 InvalidKeyException 방지 (Texture2D 호환)
                    var texture = await Addressables.LoadAssetAsync<Texture>(address);
                    if (texture != null && Occupant == character) // 비동기 완료 시점의 Occupant 확인
                    {
                        // 캐시에 저장
                        if (m_currentImageType == SlotImageType.Idle) character.IdlePortrait = texture;
                        else if (m_currentImageType == SlotImageType.Attack) character.AttackPortrait = texture;
                        
                        // 현재 표시 중인 타입과 일치할 때만 적용
                        m_CharacterSlotVisualizer.SetPortrait(texture);
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[CharacterSlot] Failed to load texture: {address}. Error: {ex.Message}");
                }
            }
            else
            {
                // 주소가 없는 경우 비주얼 초기화
                m_CharacterSlotVisualizer.SetPortrait(null);
            }
        }

        public void Clear()
        {
            if (Occupant != null)
            {
                Occupant.OnDamageTaken -= ShowDamageText;
                Occupant.OnHealed -= ShowHealText;
                Occupant.OnEvaded -= ShowEvadedText;
                Occupant.OnHpChanged -= UpdateHpBar;
                Occupant.OnInsanityChanged -= UpdateInsanityBar;
                Occupant.OnBuffsChanged -= UpdateBuffs;
                Occupant.CurrentSlot = null;
            }
            Occupant = null;
            m_CharacterSlotVisualizer.SetPortrait(null); // 스프라이트 참조 제거
            if (m_CharacterSlotBar != null)
            {
                m_CharacterSlotBar.hpfillAmount = 0f;
                m_CharacterSlotBar.insfillAmount = 0f;
            }
            if (m_CharacterSlotBuffBar != null)
            {
                m_CharacterSlotBuffBar.SetBuffs(null);
            }
            if (m_CharacterSlotBar != null)
            {
                m_CharacterSlotBar.isTurnActive = false;
            }
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
        public void SetActing(bool val)
        {
            IsActing = val;
            if (m_CharacterSlotBar != null)
            {
                m_CharacterSlotBar.isTurnActive = val;
            }
        }
        public void SetTargeted(bool val) => IsTargeted = val;

        public void ResetAllStates()
        {
            IsSelected = false;
            IsTargetable = false;
            IsActing = false;
            IsTargeted = false;
        }

        public float BarAlpha
        {
            get => m_CharacterSlotBar != null ? m_CharacterSlotBar.alpha : 1f;
            set
            {
                if (m_CharacterSlotBar != null)
                {
                    m_CharacterSlotBar.alpha = value;
                }
            }
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
            // [UI 우선순위] 유저 인터랙션(마우스)을 최우선으로 처리하여 즉각적인 피드백 제공
            if (m_isPressed)
            {
                m_targetColor = colorData.pressedColor;
            }
            else if (m_isHovered)
            {
                m_targetColor = colorData.hoverColor;
            }
            // 마우스 입력이 없을 때만 논리적 상태(대상 지정, 현재 턴 등)를 표시
            else if (IsTargeted)
            {
                m_targetColor = colorData.pressedColor;
            }
            else if (IsTargetable)
            {
                m_targetColor = colorData.targetableColor;
            }
            else if (IsSelected || IsActing)
            {
                m_targetColor = colorData.hoverColor;
            }
            else
            {
                m_targetColor = m_CharacterSlotBar != null ? m_CharacterSlotBar.defaultBorderColor : colorData.normalColor;
            }

            m_currentColor = Color.Lerp(m_currentColor, m_targetColor, Time.unscaledDeltaTime * colorData.lerpSpeed);
            m_CharacterSlotVisualizer.SetCurrentColor(m_currentColor);
            if (m_CharacterSlotBar != null)
            {
                m_CharacterSlotBar.currentColor = m_currentColor;
            }
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

        private void ShowDamageText(BaseCharacter target, int amount, bool isCritical)
        {
            if (m_TextVisualizer != null)
            {
                m_TextVisualizer.Show(amount, isHeal: false, isCritical: isCritical);
            }
        }

        private void ShowHealText(BaseCharacter target, int amount, bool isCritical)
        {
            if (m_TextVisualizer != null)
            {
                m_TextVisualizer.Show(amount, isHeal: true, isCritical: isCritical);
            }
        }

        private void ShowEvadedText(BaseCharacter target)
        {
            if (m_TextVisualizer != null)
            {
                m_TextVisualizer.ShowMiss();
            }
        }

        private void UpdateHpBar(BaseCharacter character)
        {
            if (m_CharacterSlotBar != null && character != null)
            {
                m_CharacterSlotBar.hpfillAmount = character.Stat.max_Hp > 0 
                    ? (float)character.Stat.current_Hp / character.Stat.max_Hp 
                    : 0f;
            }
        }

        private void UpdateInsanityBar(BaseCharacter character)
        {
            if (m_CharacterSlotBar != null && character != null)
            {
                m_CharacterSlotBar.insfillAmount = (float)character.Insanity / 100f;
            }
        }

        private void UpdateBuffs(BaseCharacter character)
        {
            if (m_CharacterSlotBuffBar != null && character != null)
            {
                m_CharacterSlotBuffBar.SetBuffs(character.ActiveBuffs);
            }
        }

        private void OnDestroy()
        {
            if (Occupant != null)
            {
                Occupant.OnDamageTaken -= ShowDamageText;
                Occupant.OnHealed -= ShowHealText;
                Occupant.OnEvaded -= ShowEvadedText;
                Occupant.OnHpChanged -= UpdateHpBar;
                Occupant.OnInsanityChanged -= UpdateInsanityBar;
                Occupant.OnBuffsChanged -= UpdateBuffs;
            }
        }
    }
}
