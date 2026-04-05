using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

namespace juno_Test
{
    public class TestPlayer : MonoBehaviour, ITurnUseUnit
    {
        [SerializeField] private string unitName;
        [SerializeField] private int speed;
        [SerializeField] private string imageAddress;
        private Juno_TestInput _input;
        public int Speed => speed;
        public bool IsDead { get; private set; } = false;
        public string ImageAddress => imageAddress;
        public int RandomSpeed { get; set; }

        private AutoResetUniTaskCompletionSource<bool> _tcs;

        private void Start()
        {
            _input = new Juno_TestInput();
            _input.Space.space.performed += OnActionButtonClicked;
            _input.Space.FkeyDie.performed += OnDie;
            _input.Enable();
        }


        private void OnDestroy()
        {
            _input.Space.space.performed -= OnActionButtonClicked;
            _input.Space.FkeyDie.performed -= OnDie;
            _input.Dispose();
        }

        public async UniTask TakeTurnAsync()
        {
            Debug.Log($"<color=green>{unitName} 차례! 플레이어의 명령을 기다립니다...</color>");

            _tcs = AutoResetUniTaskCompletionSource<bool>.Create();

            await _tcs.Task;
            _tcs = null;

            Debug.Log($"{unitName} 행동 완료!");
        }

        private void OnDie(InputAction.CallbackContext context)
        {
            if (_tcs == null) return;
            IsDead = true;
            Debug.Log($"<color=red>[테스트] {unitName} 강제 사망!</color>");
            _tcs?.TrySetResult(true);
        }

        private void OnActionButtonClicked(InputAction.CallbackContext context)
        {
            if (_tcs == null) return;
            _tcs?.TrySetResult(true);
        }

        public int CompareTo(ITurnUseUnit other)
        {
            if (other == null) return 1;
            // 1. 먼저 스피드를 비교
            int speedComparison = other.Speed.CompareTo(this.Speed);

            // 2. 만약 스피드가 완전히 똑같다면 
            if (speedComparison == 0)
            {
                // 3. 매니저가 나누어준 랜덤 번호표로 순서를 결정
                return other.RandomSpeed.CompareTo(this.RandomSpeed);
            }

            // 스피드가 다르면 그냥 스피드 비교 결과를 반환
            return speedComparison;
        }
    }
}