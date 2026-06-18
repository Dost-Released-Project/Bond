using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 컷씬 씬에서 CutSceneLoader 로 완료 신호를 전달하는 단방향 채널.
///
/// StageCompletionChannel 과 동일한 스택 기반 패턴이지만 StageResult 없이
/// 단순 Action 으로 완료를 통지한다.
///
/// 사용 흐름:
///   1. CutSceneLoader.Load() 씬 로드 직전 → Register(callback)
///   2. SkillCutSceneController → Invoke()  ← Peek() 대상 콜백 호출
///   3. CutSceneLoader.Load() 언로드 완료 → Unregister()  ← Pop
/// </summary>
public static class CutSceneCompletionChannel
{
    private static readonly Stack<Action> _callbacks = new Stack<Action>();

    /// <summary>
    /// 완료 콜백을 스택에 Push 한다.
    /// </summary>
    /// <param name="callback">컷씬 완료 시 실행할 콜백.</param>
    public static void Register(Action callback)
    {
        _callbacks.Push(callback);
    }

    /// <summary>
    /// 스택 최상위(Peek) 콜백을 호출한다.
    /// 등록된 콜백이 없으면 경고 로그를 출력하고 반환한다.
    /// </summary>
    public static void Invoke()
    {
        if (_callbacks.Count == 0)
        {
            Debug.LogWarning("[CutSceneCompletionChannel] 등록된 콜백이 없습니다.");
            return;
        }

        _callbacks.Peek().Invoke();
    }

    /// <summary>
    /// 스택 최상위 콜백을 Pop 한다.
    /// 스택이 비어 있으면 경고 로그를 출력하고 반환한다.
    /// </summary>
    public static void Unregister()
    {
        if (_callbacks.Count == 0)
        {
            Debug.LogWarning("[CutSceneCompletionChannel] Unregister 호출됐으나 스택이 비어 있습니다.");
            return;
        }

        _callbacks.Pop();
    }
}
