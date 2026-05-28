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
    public void FlushItemRewardLog(string itemDisplayName)
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
    /// 런 종료 시 외부에서 명시적으로 호출한다.
    /// 자동 Clear는 하지 않는다.
    /// </summary>
    public void Clear()
    {
        _allLogs.Clear();
    }
}
