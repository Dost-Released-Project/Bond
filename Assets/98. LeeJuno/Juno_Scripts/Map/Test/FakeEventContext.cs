using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 테스트 환경에서 IEventContext 를 대체하는 더미.
/// Inspector 에서 EventData SO 를 직접 지정해 StageLoader 없이 선택지를 공급한다.
///
/// MonoBehaviour 로 구현한 이유:
/// Inspector 에서 EventData SO 를 드래그로 연결하려면 MonoBehaviour 가 필요하다.
/// 순수 C# 클래스로는 SO 연결이 불가하다.
/// </summary>
public class FakeEventContext : MonoBehaviour, IEventContext
{
    [SerializeField] private EventData _testEventData;
    [SerializeField] private EventBattleConfig _testBattleConfig;

    private string _eventId = "TEST_EVENT_001";
    private string _description = string.Empty;
    private List<EventChoice> _choices = new List<EventChoice>();
    private EventBattleConfig _battleConfig;

    public string EventId => _eventId;
    public string Description => _description;
    public IReadOnlyList<EventChoice> Choices => _choices;
    public EventBattleConfig BattleConfig => _battleConfig;

    /// <summary>
    /// 인스펙터에서 연결된 _testEventData 를 반환한다.
    /// Set() 호출 시 전달된 eventData 는 무시하고 인스펙터 연결 값을 우선한다.
    /// </summary>
    public EventData EventData => _testEventData;

    private void Awake()
    {
        if (_testEventData == null)
        {
            Debug.LogWarning("[FakeEventContext] _testEventData 가 연결되지 않았습니다. 더미 선택지로 대체합니다.");
            _choices      = BuildDummyChoices();
            _battleConfig = _testBattleConfig;
            return;
        }

        _eventId      = _testEventData.Id;
        _description  = _testEventData.Description;
        _choices      = new List<EventChoice>(_testEventData.Choices);
        _battleConfig = _testBattleConfig;
    }

    public void Set(string eventId, string description, List<EventChoice> choices, EventBattleConfig battleConfig, EventData eventData)
    {
        _eventId      = eventId;
        _description  = description;
        _choices      = new List<EventChoice>(choices);
        _battleConfig = battleConfig;
        // eventData 는 무시 — 인스펙터 연결된 _testEventData 를 EventData 프로퍼티로 노출
    }

    public void Clear()
    {
        _eventId      = string.Empty;
        _description  = string.Empty;
        _choices.Clear();
        _battleConfig = null;
    }

    /// <summary>
    /// EventData SO 미연결 시 사용하는 하드코딩 더미 선택지.
    /// </summary>
    private List<EventChoice> BuildDummyChoices()
    {
        EventChoice choiceA = new EventChoice();
        choiceA.SetData("앞으로 나아간다", "용기 있는 선택이었다.", null);

        EventChoice choiceB = new EventChoice();
        choiceB.SetData("뒤로 물러난다", "신중한 선택이었다.", null);

        return new List<EventChoice> { choiceA, choiceB };
    }
}
