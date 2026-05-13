
using System.Collections.Generic;
using Bond.Persistence;

public class Roster : ISaveable<List<BaseCharacter>>
{
    public List<BaseCharacter> Characters = new List<BaseCharacter>();
    public int Max = 20;
    public bool IsFull => Characters.Count >= Max;

    public bool Hire(BaseCharacter character)
    {
        if (IsFull || Characters.Contains(character))
        {
            return false;
        }
        else
        {
            Characters.Add(character);
            return true;
        }
    }
    
    public bool Fire(BaseCharacter character)
    {
        return Characters.Remove(character);
    }

    public string Key => "roster";
    public List<BaseCharacter> Data => Characters;
    public void Restore(List<BaseCharacter> data)
    {
        Characters = data;
    }
}