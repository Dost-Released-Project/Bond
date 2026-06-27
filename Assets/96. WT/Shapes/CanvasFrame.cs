using UnityEngine;
using Shapes;

namespace _02.Scripts.UI.Title
{
    /// <summary>
    /// [V] Title 씬 전용 Shapes UI 캔버스.
    /// 화면 테두리와 UI 영역 시각화를 담당하며, 카메라 자동 연결 기능을 포함합니다.
    /// </summary>
    [ExecuteAlways]
    public class CanvasFrame : ImmediateModeCanvas
    {
        [Header("Frame Settings")]
        public float cornerRadius = 20f;
        public float thickness = 10f;
        public float padding = 40f; 

        [Header("Color Settings")]
        [ColorUsage(true, true)] public Color frameColor = Color.white;
        [ColorUsage(true, true)] public Color backgroundColor = new Color(0, 0, 0, 0.3f);

        public override void OnEnable()
        {
            base.OnEnable();
            
            // Canvas 컴포넌트를 가져와 worldCamera를 강제로 할당합니다.
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null)
            {
                canvas.worldCamera = Camera.main;
                Debug.Log("[TitleShapesCanvas] 메인 카메라를 Canvas에 강제 할당했습니다.");
            }
        }

        public override void DrawCanvasShapes(ImCanvasContext ctx)
        {
            // [방어 코드] OnEnable 시점에 카메라 로드 지연으로 바인딩되지 못했을 경우 실시간 대응
            Canvas canvas = GetComponent<Canvas>();
            if (canvas != null && canvas.worldCamera == null)
            {
                canvas.worldCamera = Camera.main;
            }

            // 화면 영역 계산
            Rect drawRect = Inset(ctx.canvasRect, padding);

            // 1. 반투명 배경
            Draw.Rectangle(drawRect, cornerRadius, backgroundColor);

            // 2. 부드러운 외곽 테두리
            Draw.RectangleBorder(drawRect, thickness, cornerRadius, frameColor);

            // 3. 하위 요소들 출력
            base.DrawPanels();
        }

        private Rect Inset(Rect r, float amount)
        {
            return new Rect(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);
        }
    }
}
