using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IEventContext 구현체.
/// MapLifetimeScope Singleton으로 등록되어 맵 씬 생명주기 동안 유지된다.
/// 기존 EventContext 정적 클래스의 로직을 인스턴스 기반으로 이전한 구현이다.
/// </summary>
public class EventContextService : IEventContext
{
    private string _eventId = string.Empty;
    private string _description = string.Empty;
    private List<EventChoice> _choices = new List<EventChoice>();
    private EventBattleConfig _battleConfig;
    private EventData _eventData;

    /// <summary>현재 배정된 이벤트 ID.</summary>
    public string EventId => _eventId;

    /// <summary>현재 이벤트의 설명 텍스트.</summary>
    public string Description => _description;

    /// <summary>현재 이벤트의 선택지 목록 (읽기 전용).</summary>
    public IReadOnlyList<EventChoice> Choices => _choices;

    /// <summary>이벤트 전투 씬 전환에 필요한 Config.</summary>
    public EventBattleConfig BattleConfig => _battleConfig;

    /// <summary>
    /// 현재 이벤트의 EventData SO 참조.
    /// EventJournalProvider 가 JournalDataSO 에 직접 접근하기 위해 사용한다.
    /// </summary>
    public EventData EventData => _eventData;

    /// <summary>
    /// StageLoader 가 Event 스테이지 씬 로드 직전에 호출한다.
    /// 기존 데이터를 덮어쓰고 choices 는 방어적 복사를 수행한다.
    /// </summary>
    /// <param name="eventId">배정된 이벤트 ID.</param>
    /// <param name="description">이벤트 설명 텍스트.</param>
    /// <param name="choices">이벤트의 선택지 목록.</param>
    /// <param name="battleConfig">이벤트 전투 Config. Battle 선택지가 없으면 null.</param>
    /// <param name="eventData">현재 이벤트 EventData SO. JournalDataSO 직접 참조용.</param>
    public void Set(string eventId, string description, List<EventChoice> choices, EventBattleConfig battleConfig, EventData eventData)
    {
        _eventId = eventId;
        _description = description;
        // 외부 리스트 변경이 내부 상태에 영향을 미치지 않도록 방어적 복사
        _choices = new List<EventChoice>(choices);
        _battleConfig = battleConfig;
        _eventData    = eventData;
        // TODO: 검증 완료 후 제거
        Debug.Log($"[EventContextService] Set → EventId='{_eventId}', Choices={_choices.Count}, BattleConfig={(_battleConfig != null ? "있음" : "null")}");
    }

    /// <summary>
    /// 이벤트 씬의 진입점(EventSceneController)에서 데이터를 읽은 뒤 호출한다.
    /// 호출하지 않으면 다음 스테이지 진입 시 이전 데이터가 남아 있을 수 있다.
    /// </summary>
    public void Clear()
    {
        _eventId = string.Empty;
        _description = string.Empty;
        _choices.Clear();
        _battleConfig = null;
        _eventData    = null;
        // TODO: 검증 완료 후 제거
        Debug.Log("[EventContextService] Clear 호출됨");
    }
}
