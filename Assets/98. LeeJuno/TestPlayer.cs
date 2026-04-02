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

        private AutoResetUniTaskCompletionSource<bool> _tcs;

        public async UniTask TakeTurnAsync()
        {
            Debug.Log($"<color=green>{unitName} 차례! 플레이어의 명령을 기다립니다...</color>");

            // 새로운 TCS 생성 (이전 턴의 흔적을 없애기 위해 매번 새로 만듦)
            _tcs = AutoResetUniTaskCompletionSource<bool>.Create();

            await _tcs.Task;

            Debug.Log($"{unitName} 행동 완료!");
        }

        private void OnActionButtonClicked()
        {
            // 이 코드가 실행되는 순간, 아까 멈춰있던 'await _tcs.Task;'가 풀리고 다음 줄로 넘어가!
            _tcs?.TrySetResult(true);
        }

        public int CompareTo(ITurnUseUnit other)
        {
            if (other == null) return 1;
            return other.Speed.CompareTo(Speed);
        }
    }
}