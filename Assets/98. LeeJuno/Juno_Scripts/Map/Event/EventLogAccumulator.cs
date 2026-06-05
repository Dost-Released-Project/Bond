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
    private readonly List<string> _pendingDeathLines = new List<string>();
    private readonly MapConfigCache _mapConfigCache;

    /// <summary>
    /// ItemRewardEventEffectHandler 가 HandleAsync() 에서 이벤트 이름을 참조할 수 있도록
    /// EventSceneController 가 RecordChoice() 호출 직전에 예약해 두는 이름.
    /// </summary>
    private string _pendingEventName    = string.Empty;
    private JournalReport _pendingReport = null;

    /// <summary>
    /// Fallback 경로에서 "선택: {Label}" 단락이 추가된 인덱스.
    /// AppendChoiceLabelTarget() 에서 대상 이름을 덧붙일 때 사용한다.
    /// -1 이면 Label 단락 없음(Template 경로 또는 Label 이 비어있는 경우).
    /// </summary>
    private int _pendingChoiceLabelIndex = -1;

    /// <summary>
    /// ItemRewardEventEffectHandler 가 아이템 확정 후 기록하는 표시 이름.
    /// ApplyEffectAndCompleteAsync 에서 OutcomeDescription 앞에 붙이기 위해 사용한다.
    /// BeginPendingReport 호출 시 초기화된다.
    /// </summary>
    public string LastResolvedItemDisplayName { get; private set; } = string.Empty;

    /// <summary>
    /// ItemRewardEventEffectHandler 가 아이템 확정 시 호출한다.
    /// </summary>
    public void SetLastResolvedItemDisplayName(string displayName)
    {
        LastResolvedItemDisplayName = displayName ?? string.Empty;
    }

    public EventLogAccumulator(MapConfigCache mapConfigCache)
    {
        _mapConfigCache = mapConfigCache;
    }

    public IReadOnlyList<JournalReport> AllLogs => _allLogs;

    public bool HasLogs => _allLogs.Count > 0;
    

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
        string logText   = $"{eventName}에서 {itemDisplayName}을 획득하였습니다.";

        // 별도 Report 를 만들지 않고 진행 중인 이벤트 기록 Pending Report 에 추가한다
        // CommitPendingReport() 는 ApplyEffectAndCompleteAsync 또는 ExecuteSecondaryActionAndCompleteAsync 에서 호출한다
        AppendToPendingReport(logText);
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
    /// 이벤트 선택 시점에 Pending Report 를 열고 선택지 텍스트를 기록한다.
    /// EffectType.ItemReward 는 FlushItemRewardLog*/FlushItemRewardLogById 에서 별도 처리하므로 건너뛴다.
    /// AppendToPendingReport() 로 추가 단락을 덧붙인 뒤 CommitPendingReport() 로 확정한다.
    /// </summary>
    /// <param name="eventData">현재 이벤트 EventData SO. null 허용 — Fallback 처리됨.</param>
    /// <param name="choice">플레이어가 선택한 EventChoice.</param>
    /// <param name="choiceIndex">JournalDataSO.Options 와의 인덱스 매핑용.</param>
    public void BeginPendingReport(EventData eventData, EventChoice choice, int choiceIndex)
    {
        if (choice == null)
            return;

        LastResolvedItemDisplayName = string.Empty;

        JournalDataSO template = eventData != null ? eventData.JournalData : null;

        if (template != null)
        {
            _pendingChoiceLabelIndex = -1;

            // Title 을 Paragraphs 첫 번째 줄로 추가한다 — JournalModel 이 Title 필드를 표시하지 않으므로 Paragraphs 에 포함한다
            List<string> paragraphs = new List<string> { "이벤트 기록" };
            paragraphs.AddRange(template.Paragraphs);
            List<JournalOption> options = new List<JournalOption>(template.Options);

            JournalOption? selectedOption = null;
            if (choiceIndex >= 0 && choiceIndex < options.Count)
                selectedOption = options[choiceIndex];

            _pendingReport = new JournalReport
            {
                Title      = "이벤트 기록",
                Paragraphs = paragraphs,
                IconId     = template.EntryIconId,
                // 이력 열람용 — 재선택 불필요하므로 Options 를 비운다
                Options    = new List<JournalOption>(),
                ProviderId = "EventJournal",
            };

            if (selectedOption.HasValue)
                _pendingReport.SelectedOption = selectedOption;
        }
        else
        {
            // Fallback: JournalDataSO 가 연결되지 않은 경우 EventChoice 필드 직접 사용
            // Title 을 Paragraphs 첫 번째 줄로 추가한다 — JournalModel 이 Title 필드를 표시하지 않으므로 Paragraphs 에 포함한다
            List<string> paragraphs = new List<string> { "이벤트 기록" };

            if (string.IsNullOrEmpty(choice.Label) == false)
            {
                _pendingChoiceLabelIndex = paragraphs.Count;
                paragraphs.Add($"선택: {choice.Label}");
            }

            if (string.IsNullOrEmpty(choice.OutcomeDescription) == false)
                paragraphs.Add(choice.OutcomeDescription);

            if (paragraphs.Count == 0)
                paragraphs.Add("이벤트에서 선택을 마쳤다.");

            _pendingReport = new JournalReport
            {
                Title      = "이벤트 기록",
                Paragraphs = paragraphs,
                IconId     = string.Empty,
                Options    = new List<JournalOption>(),
                ProviderId = "EventJournal",
            };
        }
    }

    /// <summary>
    /// Pending Report 에 HP 변화 등 추가 단락을 덧붙인다.
    /// BeginPendingReport() 가 호출된 경우에만 동작하며, null 또는 빈 문자열은 무시한다.
    /// </summary>
    /// <param name="line">추가할 단락 텍스트.</param>
    public void AppendToPendingReport(string line)
    {
        if (_pendingReport == null || string.IsNullOrEmpty(line))
            return;

        _pendingReport.Paragraphs.Add(line);
    }

    /// <summary>
    /// Pending Report 를 _allLogs 에 추가하고 초기화한다.
    /// 모든 효과 적용이 끝난 시점(ApplyEffectAndCompleteAsync, HandleBattleChoice 등)에 호출한다.
    /// </summary>
    public void CommitPendingReport()
    {
        if (_pendingReport == null)
            return;

        _allLogs.Add(_pendingReport);
        _pendingReport           = null;
        _pendingChoiceLabelIndex = -1;
    }

    /// <summary>
    /// Pending Report 를 _allLogs 에 추가하지 않고 버린다.
    /// 예외 처리나 취소 경로에서 누수 방지를 위해 사용한다.
    /// </summary>
    public void CancelPendingReport()
    {
        _pendingReport           = null;
        _pendingChoiceLabelIndex = -1;
    }

    /// <summary>
    /// ChooseOne 효과 처리 후 선택지 Label 단락에 대상 이름을 덧붙인다.
    /// BeginPendingReport() 의 Fallback 경로에서 Label 단락이 기록된 경우에만 동작한다.
    /// </summary>
    /// <param name="targetName">선택된 대상 캐릭터 이름.</param>
    public void AppendChoiceLabelTarget(string targetName)
    {
        if (_pendingReport == null)
            return;

        if (string.IsNullOrEmpty(targetName))
            return;

        if (_pendingChoiceLabelIndex < 0 || _pendingChoiceLabelIndex >= _pendingReport.Paragraphs.Count)
            return;

        _pendingReport.Paragraphs[_pendingChoiceLabelIndex] += $" → {targetName}";
    }

    /// <summary>
    /// 이벤트 전투 결과를 열려있는 Pending Report 에 덧붙이고 확정한다.
    /// 이벤트 선택 기록과 전투 결과가 하나의 Report 로 합쳐진다.
    /// Pending Report 가 없으면 RecordBattleResult() 로 단독 기록한다.
    /// </summary>
    /// <param name="result">전투 완료 결과.</param>
    /// <param name="monsterGroupId">전투 상대 몬스터 그룹 ID. 없으면 string.Empty.</param>
    public void AppendBattleResultToPendingReport(StageResult result, string monsterGroupId)
    {
        if (_pendingReport == null)
        {
            // Pending Report 가 없으면 단독 전투 기록으로 폴백한다
            RecordBattleResult(result, monsterGroupId);
            return;
        }

        string groupDisplayName = FindMonsterGroupName(monsterGroupId);
        string groupPart   = string.IsNullOrEmpty(groupDisplayName) ? "전투" : $"'{groupDisplayName}'";
        string outcomePart = result.IsSuccess ? "승리" : "패배";
        string logText     = $"{groupPart}에게 {outcomePart}하였습니다.";

        AppendToPendingReport(logText);
        CommitPendingReport();
    }

    /// <summary>
    /// 전투 결과를 즉시 _allLogs 에 추가한다.
    /// monsterGroupId 로 MonsterGroupConfig 에서 그룹 표시 이름을 조회한다.
    /// </summary>
    /// <param name="result">StageLoader 가 수신한 전투 완료 결과.</param>
    /// <param name="monsterGroupId">전투 상대 몬스터 그룹 ID. 없으면 string.Empty.</param>
    public void RecordBattleResult(StageResult result, string monsterGroupId)
    {
        string groupDisplayName = FindMonsterGroupName(monsterGroupId);

        string groupPart   = string.IsNullOrEmpty(groupDisplayName) ? "전투" : $"'{groupDisplayName}'";
        string outcomePart = result.IsSuccess ? "승리" : "패배";
        string logText     = $"{groupPart}에게 {outcomePart}하였습니다.";

        // Title 을 Paragraphs 첫 번째 줄로 추가한다 — JournalModel 이 Title 필드를 표시하지 않으므로 Paragraphs 에 포함한다
        string battleTitle = result.IsSuccess ? "전투 승리" : "전투 패배";
        JournalReport report = new JournalReport
        {
            Title      = battleTitle,
            Paragraphs = new List<string> { battleTitle, logText },
            IconId     = string.Empty,
            Options    = new List<JournalOption>(),
            ProviderId = "BattleResult",
        };

        _allLogs.Add(report);
    }

    /// <summary>
    /// monsterGroupId 에 대응하는 MonsterGroupData 의 DisplayName 을 반환한다.
    /// 찾지 못하면 string.Empty 를 반환한다.
    /// </summary>
    private string FindMonsterGroupName(string monsterGroupId)
    {
        if (string.IsNullOrEmpty(monsterGroupId))
            return string.Empty;

        // 일반 전투 그룹 검색
        MonsterGroupConfig config = _mapConfigCache?.MonsterGroupConfig;
        if (config != null && config.Groups != null)
        {
            foreach (MonsterGroupData group in config.Groups)
            {
                if (group != null && group.Id == monsterGroupId)
                    return group.DisplayName;
            }
        }

        // 이벤트 전투 그룹은 MonsterGroupConfig 가 아닌 EventBattleConfig.MonsterGroupPool 에 별도 등록되므로 추가 검색한다
        EventBattleConfig eventBattleConfig = _mapConfigCache?.EventBattleConfig;
        if (eventBattleConfig != null && eventBattleConfig.MonsterGroupPool != null)
        {
            foreach (MonsterGroupData group in eventBattleConfig.MonsterGroupPool)
            {
                if (group != null && group.Id == monsterGroupId)
                    return group.DisplayName;
            }
        }

        return string.Empty;
    }

    public void RecordCharacterDeath(string characterName, StageType? stageType)
    {
        string context;
        if (stageType == null)
            context = "탐사 중";
        else if (stageType == StageType.Normal)
            context = "전투에서";
        else
            context = "이벤트에서";

        _pendingDeathLines.Add($"{characterName}가 {context} 전사하였습니다.");
    }

    public void RecordRetreat(bool isWipeout)
    {
        string title   = isWipeout ? "파티 전멸" : "탐사 퇴각";
        string summary = isWipeout
            ? "모든 파티원이 전사하여 탐사를 중단하였습니다."
            : "탐사대가 스스로 퇴각을 결정하였습니다.";

        List<string> paragraphs = new List<string> { title };

        foreach (string deathLine in _pendingDeathLines)
            paragraphs.Add(deathLine);

        paragraphs.Add(summary);

        JournalReport report = new JournalReport
        {
            Title      = title,
            Paragraphs = paragraphs,
            IconId     = string.Empty,
            Options    = new List<JournalOption>(),
            ProviderId = "Retreat",
        };

        _allLogs.Add(report);
        _pendingDeathLines.Clear();
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
