using UnityEngine;

public interface ICharacterSlotVisualizer
{
    public void SetBG(Color bgColor);
    public void SetCurrentColor(Color currentColor);
    public void SetPortrait(Texture texture);
}
