
using System;
using System.Collections.Generic;
using Bond.Persistence;

public class Roster : ISaveable<List<BaseCharacter>>
{
    public List<BaseCharacter> Characters = new List<BaseCharacter>();
    public int Max = 20;
    public bool IsFull => Characters.Count >= Max;
    public event Action<BaseCharacter> OnCharacterAdded;
    public event Action<BaseCharacter> OnCharacterRemoved;

    public bool Hire(BaseCharacter character)
    {
        if (IsFull || Characters.Contains(character))
        {
            return false;
        }
        else
        {
            Characters.Add(character);
            OnCharacterAdded?.Invoke(character);
            return true;
        }
    }
    
    public bool Fire(BaseCharacter character)
    {
        bool reVal = Characters.Remove(character);
        OnCharacterRemoved?.Invoke(character);
        return reVal;
    }

    public string Key => "roster";
    public List<BaseCharacter> Data => Characters;
    public void Restore(List<BaseCharacter> data)
    {
        Characters = data;
    }
}