using System.Collections.Generic;
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
    private EventBattleConfig _battleConfig;
    private IEventEffectApplier _effectApplier;
    private IEventContext _eventContext;
    private EventJournalProvider _journalProvider;
    private string _currentEventId;

    /// <summary>
    /// VContainer 로부터 주입받는다.
    /// MonoBehaviour 는 생성자 주입을 사용할 수 없으므로 [Inject] 메서드 주입을 사용한다.
    /// </summary>
    /// <param name="effectApplier">이벤트 효과 적용 서비스.</param>
    /// <param name="eventContext">이벤트 컨텍스트 — StageLoader 가 씬 로드 직전에 기록한 이벤트 데이터.</param>
    /// <param name="journalProvider">이벤트 선택 결과를 JournalSystem 에 보고하는 Provider.</param>
    [Inject]
    public void Construct(
        IEventEffectApplier effectApplier,
        IEventContext eventContext,
        EventJournalProvider journalProvider)
    {
        _effectApplier   = effectApplier;
        _eventContext    = eventContext;
        _journalProvider = journalProvider;
    }

    private void Start()
    {
        // TODO: 검증 완료 후 제거
        Debug.Log($"[EventSceneController] Start — EventId='{_eventContext.EventId}', Choices={_eventContext.Choices.Count}, BattleConfig={(_eventContext.BattleConfig != null ? "있음" : "null")}");

        // 씬 로드 직후 컨텍스트를 읽고 즉시 Clear() 호출
        // 방어적 복사 — Clear() 이후에도 로컬 참조가 유효하도록
        _choices        = new List<EventChoice>(_eventContext.Choices);
        _battleConfig   = _eventContext.BattleConfig;
        _currentEventId = _eventContext.EventId; // Clear() 전에 EventId 저장 — OnChoiceSelectedFromView 에서 사용
        _eventContext.Clear();

        // EventChoicePresenter.Start() 가 먼저 실행되어 _choiceView 가 이미 주입된 경우 즉시 표시한다
        // Presenter.Start() 가 아직 실행되지 않은 경우 BindView() 호출 시 ShowChoices() 가 실행된다
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

        // 이벤트 선택 결과를 JournalSystem 에 보고한다
        // _currentEventId 는 Start() 에서 _eventContext.Clear() 전에 저장한 값이다
        _journalProvider?.RecordChoice(_currentEventId, choice);

        EventEffectData effect = choice.Effect;

        if (effect != null && effect.EffectType == EffectType.Battle)
        {
            HandleBattleChoice();
            return;
        }

        // 일반 효과 적용 — 비동기이므로 UniTask 를 Forget() 으로 실행한다
        // 람다식: 비동기 예외를 void 컨텍스트에서 명시적으로 기록하기 위해 사용
        ApplyEffectAndCompleteAsync(effect).Forget(e => Debug.LogError(e));
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
