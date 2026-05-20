using UnityEngine;

/// <summary>
/// EventTestScene 전용 부트스트래퍼.
/// Start() 에서 StageCompletionChannel 에 로그 콜백을 등록한다.
/// 선택지를 클릭하면 StageResult 가 콘솔에 출력되어 흐름을 검증할 수 있다.
/// </summary>
public class EventTestBootstrapper : MonoBehaviour
{
    private void Start()
    {
        // 람다식: 익명 콜백을 인라인으로 등록하기 위해 사용. 별도 메서드 불필요.
        StageCompletionChannel.Register(result =>
        {
            Debug.Log($"[EventTestBootstrapper] StageCompletion 수신 — IsSuccess={result.IsSuccess}, IsBattleTriggered={result.IsBattleTriggered}");
            StageCompletionChannel.Unregister();
        });
    }

    private void OnDestroy()
    {
        StageCompletionChannel.Unregister();
    }
}
