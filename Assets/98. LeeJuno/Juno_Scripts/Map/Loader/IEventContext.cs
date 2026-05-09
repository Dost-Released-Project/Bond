using System.Collections.Generic;

/// <summary>
/// 맵 씬이 이벤트 씬으로 전달하는 이벤트 컨텍스트 계약.
/// MapLifetimeScope Singleton으로 등록되어 StageLoader(쓰기)와
/// EventSceneController(읽기) 양쪽에서 주입받는다.
/// </summary>
public interface IEventContext
{
    /// <summary>현재 배정된 이벤트 ID.</summary>
    string EventId { get; }

    /// <summary>현재 이벤트의 선택지 목록 (읽기 전용).</summary>
    IReadOnlyList<EventChoice> Choices { get; }

    /// <summary>
    /// 이벤트 전투 씬 전환에 필요한 Config.
    /// Battle 타입 선택지가 없으면 null.
    /// </summary>
    EventBattleConfig BattleConfig { get; }

    /// <summary>
    /// StageLoader 가 Event 스테이지 씬 로드 직전에 호출한다.
    /// </summary>
    /// <param name="eventId">배정된 이벤트 ID.</param>
    /// <param name="choices">이벤트의 선택지 목록.</param>
    /// <param name="battleConfig">이벤트 전투 Config. Battle 선택지가 없으면 null.</param>
    void Set(string eventId, List<EventChoice> choices, EventBattleConfig battleConfig);

    /// <summary>
    /// 이벤트 씬의 진입점(EventSceneController)에서 데이터를 읽은 뒤 호출한다.
    /// </summary>
    void Clear();
}
