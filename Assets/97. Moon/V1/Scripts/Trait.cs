using Reactions;
using UnityEngine;
using UnityEngine.Serialization;

public enum E_TraitType
{
    None,
    Positive,
    Neutral,
    Negative,
}

[System.Serializable]
public class Trait
{
    public E_TraitType Type;
    public string Name = "Missing";
    [TextArea] public string Description = "Missing";
    // public Reaction Reaction;  // 성향에 고정된 리액션

    public override string ToString()
    {
        return $"Name: {Name}\n" +
               $"Description: {Description}";
    }
}