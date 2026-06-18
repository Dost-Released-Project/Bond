using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 스테이지 씬에서 맵 씬(StageLoader)으로 완료 결과를 전달하는 단방향 채널.
///
/// NormalStageContext / EventContext 와 동일한 단방향 채널 패턴.
/// Addressables.LoadSceneAsync 는 씬 로드 시 파라미터 직접 전달 불가하고,
/// VContainer 스코프는 씬 경계를 넘지 않으므로 정적 채널로 콜백을 보관한다.
///
/// 스택 방식을 채택해 전투씬 콜백(NotifyStageCompleted)을 유지한 채로
/// 컷씬 콜백(onCutSceneCompleted)을 최상위에 Push/Pop 할 수 있다.
///
/// 사용 흐름:
///   1. StageLoader.LoadStage() 씬 로드 직전 → Register(NotifyStageCompleted)
///   2. StageLoader.LoadSkillCutScene() 씬 로드 직전 → Register(onCutSceneCompleted)
///   3. 스테이지/컷씬 내부 진입점 → Invoke(result)  ← Peek() 대상 콜백만 호출
///   4. StageLoader.UnloadCutSceneInternal() 또는 UnloadCurrentStageInternal() → Unregister()  ← Pop
/// </summary>
public static class StageCompletionChannel
{
    private static readonly Stack<Action<StageResult>> _callbacks = new Stack<Action<StageResult>>();

    /// <summary>
    /// 완료 콜백을 스택에 Push 한다.
    /// LoadStage 가 먼저 NotifyStageCompleted 를 Push 하고,
    /// LoadSkillCutScene 이 onCutSceneCompleted 를 그 위에 Push 한다.
    /// </summary>
    /// <param name="callback">완료 시 실행할 콜백.</param>
    public static void Register(Action<StageResult> callback)
    {
        _callbacks.Push(callback);
    }

    /// <summary>
    /// 스택 최상위(Peek) 콜백을 호출한다.
    /// 등록된 콜백이 없으면 경고 로그를 출력하고 반환한다.
    /// </summary>
    /// <param name="result">스테이지/컷씬 완료 결과.</param>
    public static void Invoke(StageResult result)
    {
        if (_callbacks.Count == 0)
        {
            // 강제 퇴각(파티 전멸) 등으로 이미 언로드가 진행된 경우 콜백이 없을 수 있다 — 정상 흐름이므로 Warning 으로만 기록한다
            Debug.LogWarning("[StageCompletionChannel] 등록된 콜백이 없습니다.");
            return;
        }

        _callbacks.Peek().Invoke(result);
    }

    /// <summary>
    /// 스택 최상위 콜백을 Pop 한다.
    /// UnloadCutSceneInternal 또는 UnloadCurrentStageInternal 완료 후 호출한다.
    /// 스택이 비어 있으면 경고 로그를 출력하고 반환한다.
    /// </summary>
    public static void Unregister()
    {
        if (_callbacks.Count == 0)
        {
            Debug.LogWarning("[StageCompletionChannel] Unregister 호출됐으나 스택이 비어 있습니다.");
            return;
        }

        _callbacks.Pop();
    }
}
