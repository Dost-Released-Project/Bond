using System.Collections.Generic;
using Bond.WT.Journal;
using UnityEngine;

/// <summary>
/// 런 전체에 걸쳐 이벤트 선택 이력(JournalReport)을 누적 보관하는 저장소.
/// MapLifetimeScope에 Singleton으로 등록되어 런이 끝날 때까지 유지된다.
///
/// EventJournalProvider.Dispose() 시점에 버퍼가 이 저장소로 플러시된다.
/// 1번 키 입력 시 MapUIController가 AllLogs를 JournalModel에 재적재해 표시한다.
/// </summary>
public class EventLogAccumulator
{
    private readonly List<JournalReport> _allLogs = new List<JournalReport>();
    private readonly MapConfigCache _mapConfigCache;

    /// <summary>
    /// ItemRewardEventEffectHandler 가 HandleAsync() 에서 이벤트 이름을 참조할 수 있도록
    /// EventSceneController 가 RecordChoice() 호출 직전에 예약해 두는 이름.
    /// </summary>
    private string _pendingEventName = string.Empty;

    public EventLogAccumulator(MapConfigCache mapConfigCache)
    {
        _mapConfigCache = mapConfigCache;
    }

    public IReadOnlyList<JournalReport> AllLogs => _allLogs;

    public bool HasLogs => _allLogs.Count > 0;

    /// <summary>수집된 Report 목록을 누적 저장소에 추가한다.</summary>
    public void Accumulate(IEnumerable<JournalReport> reports)
    {
        if (reports == null)
            return;

        foreach (JournalReport report in reports)
        {
            if (report != null)
            {
                // 이력 열람 시 선택지 버튼이 렌더링되지 않도록 Options를 비운다.
                // 로그는 읽기 전용이므로 재선택이 불필요하다.
                report.Options = new List<JournalOption>();
                _allLogs.Add(report);
            }
        }
    }

    /// <summary>
    /// ItemReward 효과 적용 직전에 이벤트 이름을 예약한다.
    /// EventSceneController 가 RecordChoice() 호출 직전에 설정한다.
    /// </summary>
    /// <param name="eventName">현재 이벤트의 표시 이름.</param>
    public void SetPendingEventName(string eventName)
    {
        _pendingEventName = eventName ?? string.Empty;
    }

    /// <summary>
    /// 아이템 획득이 확정된 시점에 호출한다.
    /// "{이벤트명}에서 {아이템이름}을 획득하였습니다." 형식의 로그를 AllLogs 에 추가한다.
    /// 호출 후 _pendingEventName 을 초기화한다.
    /// </summary>
    /// <param name="itemDisplayName">AccessoryDataBaseSO 에서 조회한 아이템 표시 이름.</param>
    private void FlushItemRewardLog(string itemDisplayName)
    {
        string eventName = string.IsNullOrEmpty(_pendingEventName) ? "알 수 없는 이벤트" : _pendingEventName;
        string logText = $"{eventName}에서 {itemDisplayName}을 획득하였습니다.";

        JournalReport report = new JournalReport
        {
            Title      = "아이템 획득",
            Paragraphs = new List<string> { logText },
            IconId     = string.Empty,
            Options    = new List<JournalOption>(),
            ProviderId = "EventItemReward",
        };

        _allLogs.Add(report);
        _pendingEventName = string.Empty;
    }

    /// <summary>
    /// itemId로 AccessoryDB에서 아이템 이름을 조회해 로그를 기록한다.
    /// 1차/2차 선택지 경로 모두에서 호출된다.
    /// </summary>
    /// <param name="itemId">획득한 아이템 ID.</param>
    public void FlushItemRewardLogById(string itemId)
    {
        string itemDisplayName = itemId;

        AccessoryDataBaseSO accessoryDB = _mapConfigCache?.AccessoryDB;
        if (accessoryDB != null)
        {
            BaseSO itemSO = accessoryDB.GetSO(itemId);
            if (itemSO != null)
                itemDisplayName = itemSO.DisplayName;
            else
                Debug.LogWarning($"[EventLogAccumulator] AccessoryDB에서 '{itemId}'를 찾을 수 없습니다.");
        }
        else
        {
            Debug.LogWarning("[EventLogAccumulator] AccessoryDB가 아직 로드되지 않았습니다.");
        }

        FlushItemRewardLog(itemDisplayName);
    }

    /// <summary>
    /// 아이템 미획득 시 예약된 이벤트 이름을 수동으로 초기화한다.
    /// Probability 실패 또는 ItemPool 이 비어있는 경우 HandleAsync() 에서 호출한다.
    /// </summary>
    public void ClearPendingEventName()
    {
        _pendingEventName = string.Empty;
    }

    /// <summary>
    /// 이벤트 선택 결과를 즉시 _allLogs 에 추가한다.
    /// ItemReward 타입은 FlushItemRewardLog()/FlushItemRewardLogById() 에서 별도 처리하므로 건너뛴다.
    /// JournalDataSO 가 연결되어 있으면 그 데이터를 사용하고, 없으면 EventChoice 필드로 Fallback 처리한다.
    /// </summary>
    /// <param name="eventData">현재 이벤트 EventData SO. null 허용 — Fallback 처리됨.</param>
    /// <param name="choice">플레이어가 선택한 EventChoice.</param>
    /// <param name="choiceIndex">JournalDataSO.Options 와의 인덱스 매핑용.</param>
    public void RecordEventChoice(EventData eventData, EventChoice choice, int choiceIndex)
    {
        if (choice == null)
            return;

        EffectType effectType = choice.Effect != null ? choice.Effect.EffectType : EffectType.None;

        // ItemReward 는 FlushItemRewardLog()/FlushItemRewardLogById() 에서 별도 처리한다
        if (effectType == EffectType.ItemReward)
            return;

        JournalDataSO template = eventData != null ? eventData.JournalData : null;

        if (template != null)
        {
            List<string> paragraphs = new List<string>(template.Paragraphs);
            List<JournalOption> options = new List<JournalOption>(template.Options);

            JournalOption? selectedOption = null;
            if (choiceIndex >= 0 && choiceIndex < options.Count)
                selectedOption = options[choiceIndex];

            JournalReport report = new JournalReport
            {
                Title      = "이벤트 기록",
                Paragraphs = paragraphs,
                IconId     = template.EntryIconId,
                // 이력 열람용 — 재선택 불필요하므로 Options 를 비운다
                Options    = new List<JournalOption>(),
                ProviderId = "EventJournal",
            };

            if (selectedOption.HasValue)
                report.SelectedOption = selectedOption;

            _allLogs.Add(report);
        }
        else
        {
            // Fallback: JournalDataSO 가 연결되지 않은 경우 EventChoice 필드 직접 사용
            List<string> paragraphs = new List<string>();

            if (string.IsNullOrEmpty(choice.Label) == false)
                paragraphs.Add($"선택: {choice.Label}");

            if (string.IsNullOrEmpty(choice.OutcomeDescription) == false)
                paragraphs.Add(choice.OutcomeDescription);

            if (paragraphs.Count == 0)
                paragraphs.Add("이벤트에서 선택을 마쳤다.");

            _allLogs.Add(new JournalReport
            {
                Title      = "이벤트 기록",
                Paragraphs = paragraphs,
                IconId     = string.Empty,
                Options    = new List<JournalOption>(),
                ProviderId = "EventJournal",
            });
        }
    }

    /// <summary>
    /// 런 종료 시 외부에서 명시적으로 호출한다.
    /// 자동 Clear는 하지 않는다.
    /// </summary>
    public void Clear()
    {
        _allLogs.Clear();
    }
}
