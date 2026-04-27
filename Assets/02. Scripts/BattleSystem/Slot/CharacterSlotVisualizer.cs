using System;
using _02._Scripts.BattleSystem;
using Shapes;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.TestTools;

[ExecuteAlways]
[RequireComponent(typeof(RectTransform))]
public class CharacterSlotVisualizer : ImmediateModePanel, ICharacterSlotVisualizer
{
    [Header("Visual Settings (Shapes)")]
    public float cornerRadius = 12f;
    public float thickness = 2f;
    public float padding = 5f;
    [SerializeField][ColorUsage(true, true)] private Color m_bgColor;
    
    [SerializeField]private Color m_currentColor;
    public override void DrawPanelShapes(Rect rect, ImCanvasContext ctx)
    {
        Rect drawRect = Inset(rect, padding);
            
        // 배경과 테두리 그리기
        Draw.Rectangle(drawRect, cornerRadius, m_bgColor);
        Draw.RectangleBorder(drawRect, thickness, cornerRadius, m_currentColor);
    }
    
    private Rect Inset(Rect r, float amount)
    {
        return new Rect(r.x + amount, r.y + amount, r.width - amount * 2, r.height - amount * 2);
    }

    public void SetBG(Color bgColor)
    {
        this.m_bgColor = bgColor;
    }

    public void SetCurrentColor(Color currentColor)
    {
        this.m_currentColor = currentColor;
    }
}
