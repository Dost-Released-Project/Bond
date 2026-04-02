using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.UI;

namespace juno_Test
{
    public class TestPlayer : MonoBehaviour, ITurnUseUnit
    {
        [SerializeField] private string unitName;
        [SerializeField] private int speed;
        [SerializeField] private Button actionButton;
        public int Speed => speed;
        public bool IsDead { get; private set; } = false;
        private UniTaskCompletionSource<bool> _tcs;

        private void Start()
        {
            actionButton.gameObject.SetActive(false);
        }

        public async UniTask TakeTurnAsync()
        {
            Debug.Log($"<color=green>{unitName} 차례! 플레이어의 명령을 기다립니다...</color>");

            // 1. 내 턴이 왔으니 버튼 활성화
            actionButton.gameObject.SetActive(true);

            // 2. 새로운 TCS 생성 (이전 턴의 흔적을 없애기 위해 매번 새로 만듦)
            _tcs = new UniTaskCompletionSource<bool>();

            // 3. 버튼 클릭 이벤트에 'OnActionButtonClicked' 함수 연결
            actionButton.onClick.AddListener(OnActionButtonClicked);

            // 4. 여기서 일시 정지! 누군가 _tcs에 결과를 넣어줄 때까지 비동기 대기
            await _tcs.Task;

            // 5. 행동이 끝났으니 이벤트 리스너를 해제하고 버튼 다시 끄기
            actionButton.onClick.RemoveListener(OnActionButtonClicked);
            actionButton.gameObject.SetActive(false);

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