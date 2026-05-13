
using System;

public interface ICharacterSelector
{
    BaseCharacter Selected { get; }
    event Action<BaseCharacter> OnSelectionChanged;
    
    void Select(BaseCharacter character);
    void Deselect();
    void ToggleSelection(BaseCharacter character);
}

public class CharacterSelector : ICharacterSelector
{
    public BaseCharacter Selected { get; private set; }
    public event Action<BaseCharacter> OnSelectionChanged;
    
    public void Select(BaseCharacter character)
    {
        if (Selected == character) return;
        Selected = character;
        OnSelectionChanged?.Invoke(Selected);
    }

    public void Deselect()
    {
        Selected = null;
        OnSelectionChanged?.Invoke(Selected);
    }
    
    public void ToggleSelection(BaseCharacter character)
    {
        if (Selected == character) Deselect();
        else Select(character);
    }
}