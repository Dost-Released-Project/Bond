using UnityEngine;

[CreateAssetMenu(fileName = "ColorData", menuName = "Scriptable Objects/ColorData")]
public class ColorData : ScriptableObject
{
    [Header("Color States")]
    [ColorUsage(true, true)] public Color normalColor = new Color(1, 1, 1, 0.1f);
    [ColorUsage(true, true)] public Color bgColor = new Color(1, 1, 1, 0.1f);
    [ColorUsage(true, true)] public Color hoverColor = new Color(0, 1, 1, 0.3f);
    [ColorUsage(true, true)] public Color pressedColor = new Color(0, 1, 1, 0.6f);
    public float lerpSpeed = 15f;
}
