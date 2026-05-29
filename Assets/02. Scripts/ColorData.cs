using UnityEngine;

[CreateAssetMenu(fileName = "ColorData", menuName = "Scriptable Objects/ColorData")]
public class ColorData : ScriptableObject
{
    [Header("Color States")]
    [ColorUsage(true, true)] public Color normalColor = new Color(1, 1, 1, 0.1f);
    [ColorUsage(true, true)] public Color bgColor = new Color(1, 1, 1, 0.1f);
    [ColorUsage(true, true)] public Color hoverColor = new Color(0, 1, 1, 0.3f);
    [ColorUsage(true, true)] public Color pressedColor = new Color(0, 1, 1, 0.6f);
    [ColorUsage(true, true)] public Color targetableColor = new Color(1, 0.9f, 0, 0.4f); // 대상 지정 가능 색상 추가
    public float lerpSpeed = 15f;
}
