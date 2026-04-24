using UnityEngine;
using Shapes;

namespace _02._Scripts.BattleSystem
{
    /// <summary>
    /// [V] 전투 씬 전용 Shapes UI 캔버스.
    /// 자식 오브젝트인 CharacterSlot들을 화면에 출력하는 역할을 담당합니다.
    /// </summary>
    [ExecuteAlways]
    public class BattleShapesCanvas : ImmediateModeCanvas
    {
        public override void DrawCanvasShapes(ImCanvasContext ctx)
        {
            // 이 호출이 있어야 자식인 CharacterSlot.DrawPanelShapes()가 실행됩니다.
            base.DrawPanels();
        }
    }
}
