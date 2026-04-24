// using UnityEngine;
// using Shapes;
// using UnityEngine.EventSystems;
//
// namespace _02.Scripts.UI.Title
// {
//     /// <summary>
//     /// [V] 개별 타이틀 버튼의 Shapes 외형을 담당하는 패널.
//     /// 슬라이드 중에는 거리 기반으로 부드럽게, 정지 시에는 포커스된 버튼만 강조하는 하이브리드 방식을 지원합니다.
//     /// </summary>
//     [ExecuteAlways]
//     public class ButtonPanel : ImmediateModePanel, IPointerEnterHandler, IPointerExitHandler, IPointerDownHandler, IPointerUpHandler
//     {
//         public enum SizeMode { Fill, Fixed, AspectRatio }
//
//         [Header("외형 설정")]
//         public float cornerRadius = 12f;
//         public float thickness = 4f;
//         public float padding = 4f;
//
//         [Header("크기 및 비율 설정")]
//         public SizeMode sizeMode = SizeMode.Fill;
//         public Vector2 fixedSize = new Vector2(200, 60);
//         public float aspectRatio = 1.0f; // AspectRatio 모드에서 사용 (가로/세로)
//
//         [Header("색상 설정")]
//         [ColorUsage(true, true)] public Color normalColor = Color.white;
//         [ColorUsage(true, true)] public Color hoverColor = Color.cyan;
//         [ColorUsage(true, true)] public Color pressedColor = Color.gray;
//         [ColorUsage(true, true)] public Color disabledColor = new Color(0.3f, 0.3f, 0.3f, 0.5f);
//         [ColorUsage(true, true)] public Color backgroundColor = new Color(0, 0, 0, 0.5f);
//
//         [Header("애니메이션 설정")]
//         public float focusScale = 1.15f; // 포커스 시 커질 배율
//         public float lerpSpeed = 15f;
//
//         private Color m_TargetColor;
//         private Color m_CurrentColor;
//         private Vector3 m_CurrentScale = Vector3.one;
//         private Vector3 m_TargetScale = Vector3.one;
//         
//         private bool m_IsHovered;
//         private bool m_IsPressed;
//         private bool m_IsInteractable = true;
//         private bool m_IsFocused; // Choice UX를 위한 포커스 상태
//
//         // 실시간 거리 데이터 및 이동 상태
//         private float m_NormalizedDistance = 1.0f; 
//         private bool m_IsMoving;
//
//         public override void OnEnable()
//         {
//             base.OnEnable();
//             m_CurrentColor = normalColor;
//             m_TargetColor = normalColor;
//             m_CurrentScale = Vector3.one;
//             m_TargetScale = Vector3.one;
//         }
//
//         public void SetInteractable(bool state)
//         {
//             m_IsInteractable = state;
//             m_TargetColor = state ? normalColor : disabledColor;
//             if (!state) m_TargetScale = Vector3.one;
//         }
//
//         /// <summary>
//         /// 외부(TitleUIView)에서 포커스 상태를 주입합니다.
//         /// </summary>
//         public void SetFocus(bool focused)
//         {
//             if (!m_IsInteractable) return;
//             m_IsFocused = focused;
//             
//             // 이동 중이 아닐 때만 즉시 포커스 스케일 적용
//             if (!m_IsMoving)
//             {
//                 m_TargetScale = focused ? Vector3.one * focusScale : Vector3.one;
//             }
//         }
//
//         /// <summary>
//         /// 포커스 및 거리 데이터를 주입받아 하이브리드 방식으로 스케일을 결정합니다.
//         /// </summary>
//         /// <param name="focused">현재 중앙에 가장 가까운 버튼인가?</param>
//         /// <param name="distance">중앙으로부터의 거리 (0~1 권장)</param>
//         /// <param name="isMoving">현재 스크롤이 이동 중인가?</param>
//         public void SetUXState(bool focused, float distance, bool isMoving)
//         {
//             if (!m_IsInteractable) return;
//             
//             m_IsFocused = focused;
//             m_NormalizedDistance = Mathf.Clamp01(distance);
//             m_IsMoving = isMoving;
//
//             if (m_IsMoving)
//             {
//                 // [Continuous] 이동 중에는 거리에 따라 가변적으로 확대
//                 float dynamicScale = Mathf.Lerp(focusScale, 1.0f, m_NormalizedDistance);
//                 m_TargetScale = Vector3.one * dynamicScale;
//             }
//             else
//             {
//                 // [FocusOnly] 정지 시에는 포커스된 버튼만 고정 배율로 확대
//                 m_TargetScale = focused ? Vector3.one * focusScale : Vector3.one;
//             }
//         }
//
//         public override void DrawPanelShapes(Rect rect, ImCanvasContext ctx)
//         {
//             // 1. 색상 결정
//             if (!m_IsInteractable)
//             {
//                 m_TargetColor = disabledColor;
//             }
//             else
//             {
//                 if (m_IsPressed) m_TargetColor = pressedColor;
//                 else if (m_IsHovered || m_IsFocused || (m_IsMoving && m_NormalizedDistance < 0.3f)) 
//                     m_TargetColor = hoverColor;
//                 else m_TargetColor = normalColor;
//             }
//
//             // 2. 애니메이션 처리 (색상 및 크기)
//             m_CurrentColor = Color.Lerp(m_CurrentColor, m_TargetColor, Time.unscaledDeltaTime * lerpSpeed);
//             m_CurrentScale = Vector3.Lerp(m_CurrentScale, m_TargetScale, Time.unscaledDeltaTime * lerpSpeed);
//
//             // 3. 크기 및 위치 계산
//             Rect drawRect = Inset(rect, padding);
//             Vector2 finalSize = drawRect.size;
//
//             if (sizeMode == SizeMode.Fixed)
//             {
//                 finalSize = fixedSize;
//             }
//             else if (sizeMode == SizeMode.AspectRatio)
//             {
//                 // Rect의 가로를 기준으로 세로 비율을 맞춤
//                 finalSize.y = finalSize.x / aspectRatio;
//             }
//
//             // 스케일 애니메이션 적용
//             finalSize *= m_CurrentScale.x;
//             
//             // 중앙 기준 Rect 생성
//             Vector2 center = drawRect.center;
//             Rect scaledRect = new Rect(center.x - finalSize.x * 0.5f, center.y - finalSize.y * 0.5f, finalSize.x, finalSize.y);
//
//             // 4. 그리기
//             Draw.Rectangle(scaledRect, cornerRadius * m_CurrentScale.x, backgroundColor);
//             Draw.RectangleBorder(scaledRect, thickness, cornerRadius * m_CurrentScale.x, m_CurrentColor);
//         }
//
//         public void OnPointerEnter(PointerEventData eventData) => m_IsHovered = true;
//         public void OnPointerExit(PointerEventData eventData) => m_IsHovered = false;
//         public void OnPointerDown(PointerEventData eventData) => m_IsPressed = true;
//         public void OnPointerUp(PointerEventData eventData) => m_IsPressed = false;
//
//         private Rect Inset(Rect r, float amount)
//         {
//             return new Rect(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);
//         }
//     }
// }
