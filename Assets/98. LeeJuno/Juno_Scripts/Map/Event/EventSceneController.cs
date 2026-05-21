using System.Collections.Generic;
using Bond.WT.Journal;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 이벤트 씬의 진입점 MonoBehaviour.
/// Start() 에서 EventContext 를 읽어 선택지 데이터를 로드하고 IEventChoiceView 에 위임한다.
/// 씬 로드 시 EventContext 의 데이터를 소비하고 Clear() 를 호출한다.
///
/// UI 생성은 IEventChoiceView(EventSceneView) 에 위임하며,
/// EventChoicePresenter 가 Start() 시점에 BindView() 를 호출해 View 를 주입한다.
/// </summary>
public class EventSceneController : MonoBehaviour
{
    private IEventChoiceView _choiceView;

    private List<EventChoice> _choices;
    private string _description;
    private EventBattleConfig _battleConfig;
    private IEventEffectApplier _effectApplier;
    private IEventContext _eventContext;
    private EventJournalProvider _journalProvider;
    private IReadOnlyList<IJournalActionHandler> _actionHandlers;
    private EventData _currentEventData; // _currentEventId 대신 EventData SO 를 직접 보관 — JournalDataSO 직접 참조용
    private EventSceneState _currentState = EventSceneState.Primary;

    /// <summary>
    /// VContainer 로부터 주입받는다.
    /// MonoBehaviour 는 생성자 주입을 사용할 수 없으므로 [Inject] 메서드 주입을 사용한다.
    /// </summary>
    /// <param name="effectApplier">이벤트 효과 적용 서비스.</param>
    /// <param name="eventContext">이벤트 컨텍스트 — StageLoader 가 씬 로드 직전에 기록한 이벤트 데이터.</param>
    /// <param name="journalProvider">이벤트 선택 결과를 JournalSystem 에 보고하는 Provider.</param>
    /// <param name="actionHandlers">2차 선택지 actionKey 처리 핸들러 목록.</param>
    [Inject]
    public void Construct(
        IEventEffectApplier effectApplier,
        IEventContext eventContext,
        EventJournalProvider journalProvider,
        IReadOnlyList<IJournalActionHandler> actionHandlers)
    {
        _effectApplier   = effectApplier;
        _eventContext    = eventContext;
        _journalProvider = journalProvider;
        _actionHandlers  = actionHandlers;
    }

    private void Start()
    {
        // TODO: 검증 완료 후 제거
        Debug.Log($"[EventSceneController] Start — EventId='{_eventContext.EventId}', Choices={_eventContext.Choices.Count}, BattleConfig={(_eventContext.BattleConfig != null ? "있음" : "null")}");

        // 씬 로드 직후 컨텍스트를 읽고 즉시 Clear() 호출
        // 방어적 복사 — Clear() 이후에도 로컬 참조가 유효하도록
        _choices           = new List<EventChoice>(_eventContext.Choices);
        _description       = _eventContext.Description;
        _battleConfig      = _eventContext.BattleConfig;
        _currentEventData  = _eventContext.EventData; // Clear() 전에 EventData 저장 — OnChoiceSelectedFromView 에서 사용
        _eventContext.Clear();

        // EventChoicePresenter.Start() 가 먼저 실행되어 _choiceView 가 이미 주입된 경우 즉시 표시한다
        // Presenter.Start() 가 아직 실행되지 않은 경우 BindView() 호출 시 ShowDescription()/ShowChoices() 가 실행된다
        _choiceView?.ShowDescription(_description);
        _choiceView?.ShowChoices(_choices);
    }

    /// <summary>
    /// EventChoicePresenter 가 Start() 시점에 View 를 주입한다.
    /// MonoBehaviour Start() 와 IStartable.Start() 의 실행 순서가 보장되지 않으므로
    /// _choices 로드 여부를 확인해 방어적으로 처리한다.
    /// </summary>
    /// <param name="view">UI Toolkit 기반 이벤트 선택지 View.</param>
    public void BindView(IEventChoiceView view)
    {
        _choiceView = view;

        // Controller.Start() 가 먼저 실행되어 _choices 가 이미 채워진 경우 즉시 표시한다
        if (_choices != null && _choices.Count > 0)
        {
            _choiceView.ShowDescription(_description);
            _choiceView.ShowChoices(_choices);
        }
    }

    /// <summary>
    /// EventChoicePresenter 를 통해 View 이벤트가 전달된다.
    /// EffectType 이 Battle 이면 전투 씬 전환을 요청하고,
    /// 그 외에는 IEventEffectApplier 를 통해 효과를 적용한 뒤 스테이지를 완료한다.
    /// </summary>
    /// <param name="choice">선택된 EventChoice 데이터.</param>
    public void OnChoiceSelectedFromView(EventChoice choice)
    {
        // 중복 선택 방지 — 모든 버튼을 비활성화한다
        _choiceView?.SetInteractable(false);

        // _choices 목록에서 선택된 choice 의 인덱스를 찾아 JournalDataSO.Options 매핑에 사용한다
        int choiceIndex = _choices != null ? _choices.IndexOf(choice) : -1;

        // 이벤트 선택 결과를 JournalSystem 에 보고한다
        // _currentEventData 는 Start() 에서 _eventContext.Clear() 전에 저장한 EventData SO 참조다
        _journalProvider?.RecordChoice(_currentEventData, choice, choiceIndex);

        EventEffectData effect = choice.Effect;

        if (effect != null && effect.EffectType == EffectType.Battle)
        {
            HandleBattleChoice();
            return;
        }

        // ItemReward 타입이고 JournalDataSO 가 연결된 경우 2차 선택지 화면으로 전환한다
        if (effect != null
            && effect.EffectType == EffectType.ItemReward
            && _currentEventData != null
            && _currentEventData.JournalData != null)
        {
            // 비동기 예외를 void 컨텍스트에서 명시적으로 기록하기 위해 람다를 사용한다
            ShowSecondaryPhaseAsync(_currentEventData.JournalData).Forget(e => Debug.LogError(e));
            return;
        }

        // 일반 효과 적용 — 비동기이므로 UniTask 를 Forget() 으로 실행한다
        // 람다식: 비동기 예외를 void 컨텍스트에서 명시적으로 기록하기 위해 사용
        ApplyEffectAndCompleteAsync(effect).Forget(e => Debug.LogError(e));
    }

    /// <summary>
    /// JournalDataSO 기반 2차 선택지 화면으로 전환한다.
    /// View 의 ShowSecondaryPhase() 를 호출하고 OnSecondaryOptionSelected 핸들러를 연결한다.
    /// </summary>
    /// <param name="journalData">표시할 JournalDataSO.</param>
    private async UniTask ShowSecondaryPhaseAsync(JournalDataSO journalData)
    {
        _currentState = EventSceneState.Secondary;

        // OnSecondaryOptionSelected 핸들러를 2차 선택지 처리 메서드에 연결한다
        // 람다식: journalData 를 클로저로 캡처해 OnSecondaryChoiceSelected 에 전달하기 위해 사용한다
        _choiceView.OnSecondaryOptionSelected = option => OnSecondaryChoiceSelected(option, journalData);

        _choiceView.ShowSecondaryPhase(journalData.Paragraphs, journalData.Options);

        // View 를 다시 활성화해 2차 버튼 클릭을 허용한다
        _choiceView.SetInteractable(true);

        await UniTask.CompletedTask;
    }

    /// <summary>
    /// 2차 선택지 클릭 시 호출된다.
    /// actionKey 에 대응하는 IJournalActionHandler 를 찾아 실행한 뒤 씬을 완료한다.
    /// </summary>
    /// <param name="option">플레이어가 선택한 JournalOption.</param>
    /// <param name="journalData">현재 표시 중인 JournalDataSO (Metadata 조립용).</param>
    private void OnSecondaryChoiceSelected(JournalOption option, JournalDataSO journalData)
    {
        _choiceView?.SetInteractable(false);

        // 비동기 예외를 void 컨텍스트에서 명시적으로 기록하기 위해 람다를 사용한다
        ExecuteSecondaryActionAndCompleteAsync(option, journalData).Forget(e => Debug.LogError(e));
    }

    /// <summary>
    /// IJournalActionHandler 를 순회해 actionKey 를 처리하고 씬을 완료한다.
    /// JournalInventoryActionHandler 는 JournalReport.Metadata["ItemId"] 를 필요로 하므로
    /// 임시 JournalReport 를 조립해서 넘긴다.
    /// </summary>
    /// <param name="option">선택된 JournalOption (actionKey 포함).</param>
    /// <param name="journalData">현재 JournalDataSO (EntryIconId 등 메타 참조용).</param>
    private async UniTask ExecuteSecondaryActionAndCompleteAsync(JournalOption option, JournalDataSO journalData)
    {
        if (string.IsNullOrEmpty(option.actionKey) == false && _actionHandlers != null)
        {
            // ItemReward 효과 데이터에서 아이템 ID 를 추출해 Metadata 에 주입한다
            // JournalInventoryActionHandler.ExecuteAction() 이 Metadata["ItemId"] 를 참조하기 때문이다
            JournalReport tempReport = BuildTempJournalReport(journalData);

            foreach (IJournalActionHandler handler in _actionHandlers)
            {
                if (handler.CanHandle(option.actionKey))
                {
                    await handler.ExecuteAction(option.actionKey, tempReport);
                }
            }
        }

        StageResult result = new StageResult
        {
            IsSuccess         = true,
            IsGameOver        = false,
            IsBattleTriggered = false,
        };

        StageCompletionChannel.Invoke(result);
    }

    /// <summary>
    /// IJournalActionHandler.ExecuteAction() 에 넘길 임시 JournalReport 를 조립한다.
    /// Metadata["ItemId"] 와 ["Quantity"] 를 현재 EventChoice 의 효과 데이터에서 채운다.
    /// </summary>
    /// <param name="journalData">현재 JournalDataSO.</param>
    /// <returns>핸들러 실행에 필요한 최소 정보를 담은 JournalReport.</returns>
    private JournalReport BuildTempJournalReport(JournalDataSO journalData)
    {
        JournalReport report = new JournalReport
        {
            Title      = "이벤트 아이템 획득",
            ProviderId = "EventSecondaryChoice",
            IconId     = journalData != null ? journalData.EntryIconId : string.Empty,
        };

        // _choices 에서 ItemReward 타입 선택지를 찾아 아이템 ID 를 추출한다
        // _choiceView.SetInteractable(false) 이후이므로 _choices 는 유효하다
        if (_choices != null)
        {
            foreach (EventChoice choice in _choices)
            {
                EventEffectData effect = choice.Effect;
                if (effect == null || effect.EffectType != EffectType.ItemReward)
                {
                    continue;
                }

                // 획득 방식에 따라 ItemId 결정
                string itemId = string.Empty;

                switch (effect.ItemRewardType)
                {
                    case ItemRewardType.Guaranteed:
                        itemId = effect.GuaranteedItemId;
                        break;
                    case ItemRewardType.Probability:
                        float roll = UnityEngine.Random.value;
                        itemId = roll <= effect.ItemProbability ? effect.ProbabilityItemId : string.Empty;
                        break;
                    case ItemRewardType.RandomFromPool:
                        if (effect.ItemPool != null && effect.ItemPool.Count > 0)
                        {
                            int poolIndex = UnityEngine.Random.Range(0, effect.ItemPool.Count);
                            itemId = effect.ItemPool[poolIndex];
                        }
                        break;
                }

                if (string.IsNullOrEmpty(itemId) == false)
                {
                    report.Metadata["ItemId"]   = itemId;
                    report.Metadata["Quantity"] = "1";
                }

                break; // ItemReward 선택지는 하나만 처리한다
            }
        }

        return report;
    }

    /// <summary>
    /// 일반 효과를 비동기로 적용하고 스테이지 완료를 알린다.
    /// </summary>
    /// <param name="effect">적용할 효과 데이터. null 이면 효과 없이 완료.</param>
    private async UniTask ApplyEffectAndCompleteAsync(EventEffectData effect)
    {
        if (effect != null && _effectApplier != null)
        {
            await _effectApplier.ApplyAsync(effect, null);
        }

        StageResult result = new StageResult
        {
            IsSuccess = true,
            IsGameOver = false,
            IsBattleTriggered = false,
        };

        StageCompletionChannel.Invoke(result);
    }

    /// <summary>
    /// Battle 타입 선택지 처리.
    /// EventBattleConfig 의 몬스터 풀에서 랜덤으로 그룹을 선택해
    /// EventBattleContext 에 기록하고 IsBattleTriggered = true 로 스테이지 완료를 알린다.
    /// </summary>
    private void HandleBattleChoice()
    {
        if (_battleConfig == null)
        {
            Debug.LogError("[EventSceneController] EventBattleConfig 가 없습니다. 전투 전환을 취소합니다.");
            _choiceView?.SetInteractable(true);
            return;
        }

        IReadOnlyList<MonsterGroupData> pool = _battleConfig.MonsterGroupPool;

        if (pool == null || pool.Count == 0)
        {
            Debug.LogError("[EventSceneController] MonsterGroupPool 이 비어 있습니다. 전투 전환을 취소합니다.");
            _choiceView?.SetInteractable(true);
            return;
        }

        // 몬스터 풀에서 랜덤으로 그룹 하나를 선택한다
        int randomIndex = UnityEngine.Random.Range(0, pool.Count);
        MonsterGroupData selectedGroup = pool[randomIndex];

        // 선택된 그룹을 EventBattleContext 에 기록한다
        // StageLoader.TransitionToEventBattleAsync() 에서 IStageMonsterContext 로 이전된다
        EventBattleContext.Set(selectedGroup.Id, selectedGroup.MonsterIds);

        StageResult result = new StageResult
        {
            IsSuccess = true,
            IsGameOver = false,
            IsBattleTriggered = true,
        };

        StageCompletionChannel.Invoke(result);
    }
}
