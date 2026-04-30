using System.Collections.Generic;

/// <summary>
/// Event 스테이지 씬이 로드되기 직전 StageLoader 가 기록하고,
/// 이벤트 씬 내부에서 읽어가는 단방향 컨텍스트 채널.
///
/// 선택 이유: Addressables.LoadSceneAsync 는 씬 로드 시 파라미터 직접 전달 불가.
/// VContainer 는 씬 경계를 넘지 않으므로 정적 컨텍스트 클래스가 가장 단순한 해결책.
///
/// 주의: 이벤트 씬의 진입점(EventSceneController)에서 데이터를 읽은 뒤 반드시 Clear() 를 호출한다.
/// </summary>
public static class EventContext
{
    /// <summary>현재 배정된 이벤트 ID.</summary>
    public static string EventId { get; private set; } = string.Empty;

    /// <summary>현재 이벤트의 선택지 목록.</summary>
    public static List<EventChoice> Choices { get; private set; } = new List<EventChoice>();

    /// <summary>
    /// StageLoader 가 Event 스테이지 씬 로드 직전에 호출한다.
    /// 기존 데이터를 덮어쓰고 choices 는 방어적 복사를 수행한다.
    /// </summary>
    /// <param name="eventId">배정된 이벤트 ID.</param>
    /// <param name="choices">이벤트의 선택지 목록.</param>
    public static void Set(string eventId, List<EventChoice> choices)
    {
        EventId = eventId;
        // 외부 리스트 변경이 내부 상태에 영향을 미치지 않도록 방어적 복사
        Choices = new List<EventChoice>(choices);
    }

    /// <summary>
    /// 이벤트 씬의 진입점(EventSceneController)에서 데이터를 읽은 뒤 호출한다.
    /// 호출하지 않으면 다음 스테이지 진입 시 이전 데이터가 남아 있을 수 있다.
    /// </summary>
    public static void Clear()
    {
        EventId = string.Empty;
        Choices.Clear();
    }
}
