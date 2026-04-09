using UnityEngine;

namespace _02._Scripts.BattleSystem
{
    /// <summary>
    /// [V] Visual: 유니티 씬에 배치되어 실제 위치와 진형 정보를 제공하는 슬롯입니다.
    /// </summary>
    public class CharacterSlot : MonoBehaviour
    {
        [Header("Slot Configuration")]
        public e_BattleSide Side;
        public FormationMask Rank;
        
        public BaseCharacter Occupant { get; private set; }

        public bool IsEmpty => Occupant == null;

        public void SetOccupant(BaseCharacter character)
        {
            Occupant = character;
        }

        public void Clear()
        {
            Occupant = null;
        }

        private void OnDrawGizmos()
        {
            // 에디터에서 시각적으로 확인하기 위한 기즈모
            Gizmos.color = (Side == e_BattleSide.Player) ? Color.cyan : Color.red;
            Gizmos.DrawWireCube(transform.position, Vector3.one * 0.5f);
            
#if UNITY_EDITOR
            UnityEditor.Handles.Label(transform.position + Vector3.up * 0.5f, $"{Side} - {Rank}");
#endif
        }
    }
}
