using System;
using UnityEngine;

/// <summary>
/// 스테이지 씬에서 맵 씬(StageLoader)으로 완료 결과를 전달하는 단방향 채널.
///
/// NormalStageContext / EventContext 와 동일한 단방향 채널 패턴.
/// Addressables.LoadSceneAsync 는 씬 로드 시 파라미터 직접 전달 불가하고,
/// VContainer 스코프는 씬 경계를 넘지 않으므로 정적 채널로 콜백을 보관한다.
///
/// 사용 흐름:
///   1. StageLoader.LoadStage() 씬 로드 직전 → Register(NotifyStageCompleted)
///   2. 스테이지 씬의 진입점(EventSceneController 등) → Invoke(result)
///   3. StageLoader.UnloadCurrentStageInternal() 언로드 완료 후 → Unregister()
/// </summary>
public static class StageCompletionChannel
{
    private static Action<StageResult> _onCompleted;

    /// <summary>
    /// StageLoader 가 씬 로드 직전에 호출한다.
    /// 기존 콜백이 있으면 덮어쓴다.
    /// </summary>
    /// <param name="callback">스테이지 완료 시 실행할 콜백.</param>
    public static void Register(Action<StageResult> callback)
    {
        _onCompleted = callback;
    }

    /// <summary>
    /// 스테이지 씬의 진입점에서 완료 결과를 전달할 때 호출한다.
    /// 콜백이 등록되지 않은 상태(null)이면 에러 로그를 출력하고 반환한다.
    /// </summary>
    /// <param name="result">스테이지 완료 결과.</param>
    public static void Invoke(StageResult result)
    {
        if (_onCompleted == null)
        {
            Debug.LogError("[StageCompletionChannel] 콜백이 등록되지 않았습니다. StageLoader.LoadStage() 이전에 Register()가 호출되었는지 확인하십시오.");
            return;
        }

        _onCompleted.Invoke(result);
    }

    /// <summary>
    /// StageLoader 가 씬 언로드 완료 후 호출한다.
    /// 콜백 참조를 null 로 초기화해 다음 사이클 이전에 잔류 참조를 제거한다.
    /// </summary>
    public static void Unregister()
    {
        _onCompleted = null;
    }
}
