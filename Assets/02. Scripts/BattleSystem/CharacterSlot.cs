using System;
using UnityEngine;
using UnityEngine.EventSystems;
using Shapes;

namespace _02._Scripts.BattleSystem
{
    /// <summary>
    /// [V] Visual: 다키스트 던전 스타일의 고정된 전투 슬롯입니다.
    /// Shapes의 ImmediateModePanel을 상속받아 UI 시스템에서 콜라이더 없이 클릭을 감지합니다.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(RectTransform))]
    public class CharacterSlot : ImmediateModePanel, 
        IPointerEnterHandler, IPointerExitHandler, 
        IPointerDownHandler, IPointerUpHandler, IPointerClickHandler
    {
        [Header("Slot Configuration")]
        public E_BattleSide side;
        public FormationMask rank;
        
        [Header("Visual Settings (Shapes)")]
        public float cornerRadius = 12f;
        public float thickness = 2f;
        public float padding = 5f;
        
        [Header("Color States")]
        [ColorUsage(true, true)] public Color normalColor = new Color(1, 1, 1, 0.1f);
        [ColorUsage(true, true)] public Color hoverColor = new Color(0, 1, 1, 0.3f);
        [ColorUsage(true, true)] public Color pressedColor = new Color(0, 1, 1, 0.6f);
        public float lerpSpeed = 15f;

        private Color m_TargetColor;
        private Color m_CurrentColor;
        private bool m_IsHovered;
        private bool m_IsPressed;

        public BaseCharacter Occupant { get; private set; }
        public bool IsEmpty => Occupant == null;

        public event Action<CharacterSlot> OnSlotClicked;

        private RectTransform m_RectTransform;
        public RectTransform RectTransform => m_RectTransform ??= GetComponent<RectTransform>();

        public override void OnEnable()
        {
            base.OnEnable();
            m_CurrentColor = normalColor;
            m_TargetColor = normalColor;
        }

        private void Update()
        {
            if (m_IsPressed) m_TargetColor = pressedColor;
            else if (m_IsHovered) m_TargetColor = hoverColor;
            else m_TargetColor = normalColor;

            m_CurrentColor = Color.Lerp(m_CurrentColor, m_TargetColor, Time.unscaledDeltaTime * lerpSpeed);
        }

        public override void DrawPanelShapes(Rect rect, ImCanvasContext ctx)
        {
            Rect drawRect = Inset(rect, padding);
            
            // 배경과 테두리 그리기
            Draw.Rectangle(drawRect, cornerRadius, new Color(0, 0, 0, 0.15f));
            Draw.RectangleBorder(drawRect, thickness, cornerRadius, m_CurrentColor);
        }

        private Rect Inset(Rect r, float amount)
        {
            return new Rect(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);
        }

        public void SetOccupant(BaseCharacter character) => Occupant = character;
        public void Clear() => Occupant = null;

        public void OnPointerEnter(PointerEventData eventData) => m_IsHovered = true;
        public void OnPointerExit(PointerEventData eventData) => m_IsHovered = false;
        public void OnPointerDown(PointerEventData eventData) => m_IsPressed = true;
        public void OnPointerUp(PointerEventData eventData) => m_IsPressed = false;
        
        public void OnPointerClick(PointerEventData eventData)
        {
            Debug.Log($"[CharacterSlot] Clicked Rank: {rank} ({side})");
            OnSlotClicked?.Invoke(this);
        }

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            // 씬 뷰에서 RectTransform의 범위를 시각적으로 표시
            if (RectTransform == null) return;

            Gizmos.matrix = RectTransform.localToWorldMatrix;
            
            // 슬롯 진영에 따른 기즈모 색상
            Color gizmoColor = (side == E_BattleSide.Player) ? Color.cyan : Color.red;
            gizmoColor.a = 0.3f;
            
            Gizmos.color = gizmoColor;
            
            // RectTransform의 중심을 기준으로 사각형 그리기
            Rect r = RectTransform.rect;
            Vector3 center = new Vector3(r.center.x, r.center.y, 0);
            Vector3 size = new Vector3(r.width, r.height, 0);
            
            Gizmos.DrawWireCube(center, size);
            
            // 상단 라벨 표시
            UnityEditor.Handles.color = Color.white;
            UnityEditor.Handles.Label(transform.position + Vector3.up * (r.height * transform.lossyScale.y * 0.5f + 10f), $"{side} - {rank}");
        }
#endif
    }
}
