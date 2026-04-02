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
        private Juno_TestInput _input;
        public int Speed => speed;
        public bool IsDead { get; private set; } = false;

        private AutoResetUniTaskCompletionSource<bool> _tcs;

        private void Start()
        {
            _input = new Juno_TestInput();
            _input.Space.space.performed += OnActionButtonClicked;
            _input.Enable();
        }


        private void OnDestroy()
        {
            _input.Space.space.performed -= OnActionButtonClicked;
            _input.Disable();
        }

        public async UniTask TakeTurnAsync()
        {
            Debug.Log($"<color=green>{unitName} 차례! 플레이어의 명령을 기다립니다...</color>");

            _tcs = AutoResetUniTaskCompletionSource<bool>.Create();

            await _tcs.Task;
            _tcs = null;

            Debug.Log($"{unitName} 행동 완료!");
        }


        private void OnActionButtonClicked(InputAction.CallbackContext context)
        {
            if (_tcs == null) return;
            _tcs?.TrySetResult(true);
        }

        public int CompareTo(ITurnUseUnit other)
        {
            if (other == null) return 1;
            return other.Speed.CompareTo(Speed);
        }
    }
}