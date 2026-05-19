using System;
using UnityEngine;

namespace Reactions
{
    [CreateAssetMenu(fileName = "TriggerPreset", menuName = "Bond/Reactions/TriggerPreset")]
    public class TriggerPreset : ScriptableObject
    {
        public E_ObserveFilter ObserveFilter;
        public E_CompareFilter SkillTargetFilter;
        [SerializeReference, SubclassSelector] public ICondition Condition;
    }
}