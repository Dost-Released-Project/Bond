using Reactions;
using UnityEngine;

[System.Serializable]
public class Trait
{
    public string traitName;
    [TextArea] public string description;
    public Trigger fixedTrigger;  // 성향에 고정된 트리거, 편집 UI에서 읽기 전용
    public SkillBase behaviour;   // 편집 가능한 행동
}