using Reactions;
using UnityEngine;
using UnityEngine.Serialization;

[System.Serializable]
public class Trait
{
    public string Name;
    [TextArea] public string Description;
    public Trigger Trigger;  // 성향에 고정된 트리거
}