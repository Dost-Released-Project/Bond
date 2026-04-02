using System;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace juno_Test
{
    public class TestPlayer : MonoBehaviour, ITurnUseUnit
    {
        [SerializeField] private string unitName;
        [SerializeField] private int speed;
        public int Speed => speed;
        public bool IsDead { get; private set; } = false;

        public async UniTask TakeTurnAsync()
        {
            Debug.Log($"{unitName}  차례");
            await UniTask.Delay(TimeSpan.FromSeconds(3f));
            Debug.Log($"{unitName} 행동 종료");
        }

        public int CompareTo(ITurnUseUnit other)
        {
            if (other == null) return 1;
            return other.Speed.CompareTo(Speed);
        }
    }
}