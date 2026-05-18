using UnityEngine;

namespace _90._HA
{
    [CreateAssetMenu(fileName = "CharacterPreset", menuName = "Bond/Character/CharacterPreset")]
    public class CharacterPreset : ScriptableObject
    {
        public BaseCharacter BaseCharacter = null;
    }
}