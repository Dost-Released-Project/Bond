using System;
using System.Collections.Generic;
using Bond.WT.Journal;
using VContainer.Unity;

/// <summary>
/// 이벤트 씬에서 플레이어의 선택 결과를 버퍼에 기록하고
/// JournalSystem 의 CollectDailyLogs() 호출 시 JournalReport 로 조립하여 제공한다.
///
/// 이벤트 씬 스코프(EventSceneLifetimeScope)에 등록되며
/// IStartable.Start() 에서 JournalSystem 에 자신을 등록하고
/// IDisposable.Dispose() 에서 등록을 해제한다.
/// </summary>
public class EventJournalProvider : IJournalContentProvider, IStartable, IDisposable
{
    /// <summary>
    /// JournalSystem 내 출력 우선순위.
    /// BattleEventProvider(30)보다 낮게 설정해 전투 이후 이벤트 순으로 출력한다.
    /// </summary>
    public int Priority => 40;

    private readonly JournalSystem _journalSystem;

    /// <summary>
    /// 이벤트 선택 결과 버퍼 내부 타입.
    /// </summary>
    private class EventChoiceRecord
    {
        public string EventId;
        public string ChoiceLabel;
        public string OutcomeDescription;
        public EffectType EffectType;
    }

    private readonly List<EventChoiceRecord> _buffer = new List<EventChoiceRecord>();

    public EventJournalProvider(JournalSystem journalSystem)
    {
        _journalSystem = journalSystem;
    }

    public void Start()
    {
        _journalSystem?.AddProvider(this);
    }

    public void Dispose()
    {
        _journalSystem?.RemoveProvider(this);
    }

    /// <summary>
    /// EventSceneController 가 선택지 클릭 시 호출한다.
    /// </summary>
    /// <param name="eventId">현재 이벤트의 ID (Start() 시점에 저장한 _currentEventId 에서 전달).</param>
    /// <param name="choice">플레이어가 선택한 EventChoice.</param>
    public void RecordChoice(string eventId, EventChoice choice)
    {
        if (choice == null)
            return;

        EffectType effectType = choice.Effect != null ? choice.Effect.EffectType : EffectType.None;

        _buffer.Add(new EventChoiceRecord
        {
            EventId            = eventId,
            ChoiceLabel        = choice.Label,
            OutcomeDescription = choice.OutcomeDescription,
            EffectType         = effectType,
        });
    }

    public IEnumerable<JournalReport> GetDailyReports()
    {
        if (_buffer.Count == 0)
            yield break;

        foreach (EventChoiceRecord record in _buffer)
        {
            List<string> paragraphs = new List<string>();

            // 선택지 레이블이 있으면 첫 문단으로 삽입
            if (string.IsNullOrEmpty(record.ChoiceLabel) == false)
                paragraphs.Add($"선택: {record.ChoiceLabel}");

            // 결과 설명이 있으면 두 번째 문단으로 삽입
            if (string.IsNullOrEmpty(record.OutcomeDescription) == false)
                paragraphs.Add(record.OutcomeDescription);

            // 두 필드 모두 비어 있으면 Fallback 텍스트
            if (paragraphs.Count == 0)
                paragraphs.Add("이벤트에서 선택을 마쳤다.");

            yield return new JournalReport
            {
                Title      = "이벤트 기록",
                Paragraphs = paragraphs,
                IconId     = string.Empty, // TODO: EventData 에 IconId 필드 추가 후 연동
                Options    = new List<JournalOption>(),
                ProviderId = "EventJournal",
            };
        }
    }

    public void ClearBuffer()
    {
        _buffer.Clear();
    }
}
