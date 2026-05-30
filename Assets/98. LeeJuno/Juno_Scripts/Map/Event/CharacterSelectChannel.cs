using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

/// <summary>
/// HpChange ChooseOne 효과 처리 시 EventScene UI 에 캐릭터 선택을 요청하는 단방향 채널.
/// HpChangeEventEffectHandler(MapLifetimeScope) ↔ EventSceneController(EventSceneLifetimeScope) 간
/// 스코프 경계를 넘는 통신에 StageCompletionChannel 과 동일한 정적 채널 패턴을 사용한다.
///
/// 사용 흐름:
///   1. HpChangeEventEffectHandler.HandleAsync → RequestAsync(names) 호출 → await 대기
///   2. EventSceneController 가 OnSelectionRequired 이벤트 수신 → View 에 캐릭터 선택 버튼 표시
///   3. 플레이어 선택 → EventSceneController → Complete(index) 호출 → 핸들러 재개
/// </summary>
public static class CharacterSelectChannel
{
    private static UniTaskCompletionSource<int> _tcs;

    /// <summary>
    /// 캐릭터 선택 UI 표시가 필요할 때 발생하는 이벤트.
    /// EventSceneController 가 구독해 View 에 선택 버튼을 생성한다.
    /// </summary>
    public static event Action<IReadOnlyList<string>> OnSelectionRequired;

    /// <summary>
    /// HpChangeEventEffectHandler 에서 호출한다.
    /// OnSelectionRequired 이벤트를 발행하고 Complete() 가 호출될 때까지 await 대기한다.
    /// </summary>
    /// <param name="characterNames">선택 가능한 파티원 이름 목록.</param>
    /// <returns>플레이어가 선택한 캐릭터의 파티 인덱스.</returns>
    public static async UniTask<int> RequestAsync(IReadOnlyList<string> characterNames)
    {
        if (OnSelectionRequired == null)
        {
            Debug.LogError("[CharacterSelectChannel] OnSelectionRequired 구독자가 없습니다. EventSceneController 가 이벤트를 구독하고 있는지 확인하십시오.");
            return 0;
        }

        _tcs = new UniTaskCompletionSource<int>();
        OnSelectionRequired.Invoke(characterNames);
        return await _tcs.Task;
    }

    /// <summary>
    /// 플레이어가 캐릭터를 선택했을 때 EventSceneController 에서 호출한다.
    /// RequestAsync() 의 await 대기를 해제한다.
    /// </summary>
    /// <param name="selectedIndex">선택된 캐릭터의 파티 인덱스.</param>
    public static void Complete(int selectedIndex)
    {
        _tcs?.TrySetResult(selectedIndex);
        _tcs = null;
    }
}
