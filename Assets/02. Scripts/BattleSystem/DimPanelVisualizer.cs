using Shapes;
using UnityEngine;

namespace BattleSystem
{
    public class DimPanelVisualizer : ImmediateModePanel
    {
        public float alpha = 0f;
        
        public override void DrawPanelShapes(Rect rect, ImCanvasContext ctx)
        {
            if (alpha <= 0f) return;
            // Canvas 전체를 덮는 반투명 검은색 사각형
            Draw.Rectangle(ctx.canvasRect, new Color(0, 0, 0, alpha));
        }
    }
}