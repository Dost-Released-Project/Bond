
using System;
using System.Collections.Generic;
using System.Linq;
using Bond.Persistence;
using UnityEngine;

public class Roster : ISaveable<List<BaseCharacter>>
{
    public List<BaseCharacter> Characters = new List<BaseCharacter>();
    public int Max = 20;
    public bool IsFull => Characters.Count >= Max;
    public event Action<BaseCharacter> OnCharacterAdded;
    public event Action<BaseCharacter> OnCharacterRemoved;

    public Roster()
    {
        SaveLoadSystem.Load(this);

        if (Characters.Count == 0)
        {
            var stageCoach = new StageCoach();
            var db = DBSORegistry.GetDb<ClassDataBaseSO>().Query<ClassSO>(so => true);

            Debug.Assert(db.Count() >= 4);

            for (int i = 0; i < 4; i++)
            {
                var chara = stageCoach.GetCharacter(db.ElementAt(i));
                Hire(chara);
            }
        }
    }
    
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
            SaveLoadSystem.Save(this);
            return true;
        }
    }
    
    public bool Fire(BaseCharacter character)
    {
        bool reVal = Characters.Remove(character);
        OnCharacterRemoved?.Invoke(character);
        SaveLoadSystem.Save(this);
        return reVal;
    }

    public string Key => "roster";
    public List<BaseCharacter> Data => Characters;
    public void Restore(List<BaseCharacter> data)
    {
        Characters = data;

        foreach (var ch in Characters)
        {
            ch.Init();
        }
    }
}