using Reactions;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Trait
{
    public string Name = "Missing";
    [TextArea] public string Description = "Missing";
    public Trigger Trigger;  // 성향에 고정된 트리거
}