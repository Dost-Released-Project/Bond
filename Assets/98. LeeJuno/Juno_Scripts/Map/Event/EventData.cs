using System.Collections.Generic;
using Bond.WT.Journal;
using UnityEngine;

/// <summary>
/// 이벤트 스테이지의 내용을 정의하는 ScriptableObject.
/// 텍스트 설명과 복수의 선택지(EventChoice)로 구성된다.
///
/// 생성 위치: Assets 우클릭 → Create → Bond → Map → EventData
/// </summary>
[CreateAssetMenu(fileName = "EventData", menuName = "Bond/Map/EventData")]
public class EventData : BaseSO
{
    [SerializeField] private List<EventChoice> _choices; // 플레이어가 선택할 수 있는 선택지 목록

    /// <summary>
    /// 인스펙터에서 직접 연결하는 JournalDataSO 참조.
    /// null 이면 EventJournalProvider 에서 Fallback(EventChoice 필드 직접 사용) 처리된다.
    /// </summary>
    [SerializeField] private JournalDataSO _journalData;

    public List<EventChoice> Choices => _choices;

    /// <summary>
    /// 인스펙터에서 연결된 JournalDataSO. null 이면 Fallback 처리된다.
    /// </summary>
    public JournalDataSO JournalData => _journalData;

    /// <summary>
    /// 파서 등 외부에서 프로그래밍 방식으로 데이터를 초기화하는 메서드.
    /// _choices 는 private 이므로 EventData 내부에서 직접 대입한다.
    /// _journalData 는 인스펙터 연결 전용이므로 SetData 에 포함하지 않는다.
    /// </summary>
    public void SetData(string id, string displayName, string description, List<EventChoice> choices)
    {
        Initialize(id, displayName, description);
        _choices = choices;
    }
}

/// <summary>
/// 이벤트 내 단일 선택지 데이터.
/// 선택 시 적용될 효과를 EventEffectData 로 정의한다.
/// </summary>
[System.Serializable]
public class EventChoice
{
    [SerializeField] private string _label;               // 선택지 버튼에 표시될 텍스트
    [SerializeField] private string _outcomeDescription;  // 선택 후 표시될 결과 설명
    [SerializeField] private EventEffectData _effect;     // 선택 시 적용될 효과 데이터

    /// <summary>선택지 버튼에 표시될 텍스트.</summary>
    public string Label => _label;

    /// <summary>선택 후 표시될 결과 설명.</summary>
    public string OutcomeDescription => _outcomeDescription;

    /// <summary>선택 시 적용될 효과 데이터.</summary>
    public EventEffectData Effect => _effect;

    /// <summary>
    /// 파서에서 호출하는 초기화 메서드.
    /// _label, _outcomeDescription, _effect 는 private 이므로 EventChoice 내부에서만 접근 가능하다.
    /// </summary>
    public void SetData(string label, string outcomeDescription, EventEffectData effect)
    {
        _label              = label;
        _outcomeDescription = outcomeDescription;
        _effect             = effect;
    }
}
