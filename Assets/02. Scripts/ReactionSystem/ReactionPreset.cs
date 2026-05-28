using UnityEngine;

namespace Reactions
{
    [CreateAssetMenu(fileName = "ReactionPreset", menuName = "Bond/Reactions/Reaction Preset")]
    public class ReactionPreset : ScriptableObject
    {
        public E_ObserveFilter ObserveFilter;
        [SerializeReference, SubclassSelector] public ITrigger Trigger;
        public string SkillId;
        public E_TargetFilter SkillTarget;
    }
}