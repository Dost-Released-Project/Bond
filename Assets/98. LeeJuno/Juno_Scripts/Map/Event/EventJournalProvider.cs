using System;
using System.Collections.Generic;
using Bond.WT.Journal;
using VContainer.Unity;

/// <summary>
/// 이벤트 씬에서 플레이어의 선택 결과를 버퍼에 기록하고
/// JournalSystem 의 CollectDailyLogs() 호출 시 JournalReport 로 조립하여 제공한다.
///
/// EventData SO 에 직접 연결된 JournalDataSO 의 Paragraphs 와 Options 를 JournalReport 에 반영한다.
/// JournalDataSO 가 null 이면 EventChoice 의 Label/OutcomeDescription 으로 Fallback 처리한다.
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
    private readonly EventLogAccumulator _logAccumulator;

    /// <summary>
    /// 이벤트 선택 결과 버퍼 내부 타입.
    /// EventData SO 를 직접 보관하여 JournalDataSO 에 즉시 접근한다.
    /// ChoiceIndex 는 JournalDataSO.Options 와의 인덱스 매핑에 사용된다.
    /// </summary>
    private class EventChoiceRecord
    {
        public EventData EventData;       // JournalDataSO 를 직접 포함하는 SO 참조
        public string ChoiceLabel;
        public string OutcomeDescription;
        public EffectType EffectType;
        public int ChoiceIndex;
    }

    private readonly List<EventChoiceRecord> _buffer = new List<EventChoiceRecord>();

    public EventJournalProvider(JournalSystem journalSystem, EventLogAccumulator logAccumulator)
    {
        _journalSystem = journalSystem;
        _logAccumulator = logAccumulator;
    }

    public void Start()
    {
        _journalSystem?.AddProvider(this);
    }

    public void Dispose()
    {
        // JournalSystem에서 Provider 해제 전에 버퍼를 누적 저장소로 플러시한다.
        // 씬 언로드 완료 후 VContainer가 스코프를 파괴하는 시점에 호출되므로
        // _buffer 데이터는 아직 유효하다.
        if (_logAccumulator != null && _buffer.Count > 0)
        {
            _logAccumulator.Accumulate(GetDailyReports());
            ClearBuffer();
        }

        _journalSystem?.RemoveProvider(this);
    }

    /// <summary>
    /// EventSceneController 가 선택지 클릭 시 호출한다.
    /// eventData 가 null 이거나 eventData.JournalData 가 null 이면 Fallback 처리된다.
    /// </summary>
    /// <param name="eventData">현재 이벤트 EventData SO. JournalData 를 직접 포함한다.</param>
    /// <param name="choice">플레이어가 선택한 EventChoice.</param>
    /// <param name="choiceIndex">_choices 목록에서 choice 의 인덱스 — JournalDataSO.Options 매핑용.</param>
    public void RecordChoice(EventData eventData, EventChoice choice, int choiceIndex)
    {
        if (choice == null)
            return;

        EffectType effectType = choice.Effect != null ? choice.Effect.EffectType : EffectType.None;

        _buffer.Add(new EventChoiceRecord
        {
            EventData          = eventData,
            ChoiceLabel        = choice.Label,
            OutcomeDescription = choice.OutcomeDescription,
            EffectType         = effectType,
            ChoiceIndex        = choiceIndex,
        });
    }

    public IEnumerable<JournalReport> GetDailyReports()
    {
        if (_buffer.Count == 0)
            yield break;

        foreach (EventChoiceRecord record in _buffer)
        {
            // ItemReward 기록은 ItemRewardEventEffectHandler/EventSceneController가 별도로 로그를 기록하므로 건너뛴다
            if (record.EffectType == EffectType.ItemReward)
                continue;

            // EventData SO 에 직접 연결된 JournalDataSO 를 참조한다. 런타임 DB 조회 불필요.
            JournalDataSO template = record.EventData != null ? record.EventData.JournalData : null;

            if (template != null)
            {
                // JournalDataSO 기반 조립
                List<string> paragraphs = new List<string>(template.Paragraphs);
                List<JournalOption> options = new List<JournalOption>(template.Options);

                // 선택한 인덱스에 해당하는 Option 을 SelectedOption 으로 기록한다
                JournalOption? selectedOption = null;
                if (record.ChoiceIndex >= 0 && record.ChoiceIndex < options.Count)
                    selectedOption = options[record.ChoiceIndex];

                JournalReport report = new JournalReport
                {
                    Title      = "이벤트 기록",
                    Paragraphs = paragraphs,
                    IconId     = template.EntryIconId,
                    Options    = options,
                    ProviderId = "EventJournal",
                };

                if (selectedOption.HasValue)
                    report.SelectedOption = selectedOption;

                yield return report;
            }
            else
            {
                // Fallback: JournalDataSO 가 연결되지 않은 경우 EventChoice 필드 직접 사용
                List<string> paragraphs = new List<string>();

                if (string.IsNullOrEmpty(record.ChoiceLabel) == false)
                    paragraphs.Add($"선택: {record.ChoiceLabel}");

                if (string.IsNullOrEmpty(record.OutcomeDescription) == false)
                    paragraphs.Add(record.OutcomeDescription);

                if (paragraphs.Count == 0)
                    paragraphs.Add("이벤트에서 선택을 마쳤다.");

                yield return new JournalReport
                {
                    Title      = "이벤트 기록",
                    Paragraphs = paragraphs,
                    IconId     = string.Empty,
                    Options    = new List<JournalOption>(),
                    ProviderId = "EventJournal",
                };
            }
        }
    }

    public void ClearBuffer()
    {
        _buffer.Clear();
    }
}
