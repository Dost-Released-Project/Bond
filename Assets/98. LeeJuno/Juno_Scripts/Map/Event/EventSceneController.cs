using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 이벤트 씬의 진입점 MonoBehaviour.
/// Start() 에서 EventContext 를 읽어 선택지 버튼을 동적으로 생성한다.
/// 씬 로드 시 EventContext 의 데이터를 소비하고 Clear() 를 호출한다.
///
/// Inspector 연결 필요:
///   _choiceContainer    — 버튼을 배치할 부모 Transform (VerticalLayoutGroup 권장)
///   _choiceButtonPrefab — EventChoiceButton 컴포넌트를 가진 버튼 프리팹
/// </summary>
public class EventSceneController : MonoBehaviour
{
    [SerializeField] private Transform _choiceContainer;
    [SerializeField] private EventChoiceButton _choiceButtonPrefab;

    private List<EventChoice> _choices;
    private EventBattleConfig _battleConfig;
    private IEventEffectApplier _effectApplier;
    private IEventContext _eventContext;

    /// <summary>
    /// VContainer 로부터 주입받는다.
    /// MonoBehaviour 는 생성자 주입을 사용할 수 없으므로 [Inject] 메서드 주입을 사용한다.
    /// </summary>
    /// <param name="effectApplier">이벤트 효과 적용 서비스.</param>
    /// <param name="eventContext">이벤트 컨텍스트 — StageLoader 가 씬 로드 직전에 기록한 이벤트 데이터.</param>
    [Inject]
    public void Construct(IEventEffectApplier effectApplier, IEventContext eventContext)
    {
        _effectApplier = effectApplier;
        _eventContext = eventContext;
        // TODO: 검증 완료 후 제거
        Debug.Log("[EventSceneController] Construct — IEventContext 주입 완료");
    }

    private void Start()
    {
        // TODO: 검증 완료 후 제거
        Debug.Log($"[EventSceneController] Start — EventId='{_eventContext.EventId}', Choices={_eventContext.Choices.Count}, BattleConfig={(_eventContext.BattleConfig != null ? "있음" : "null")}");

        // 씬 로드 직후 컨텍스트를 읽고 즉시 Clear() 호출
        // 방어적 복사 — Clear() 이후에도 로컬 참조가 유효하도록
        _choices = new List<EventChoice>(_eventContext.Choices);
        _battleConfig = _eventContext.BattleConfig;
        _eventContext.Clear();

        BuildChoiceButtons();
    }

    /// <summary>
    /// _choices 목록을 순회해 선택지 버튼을 동적으로 생성한다.
    /// 각 버튼에 EventChoice 데이터와 선택 콜백을 주입한다.
    /// </summary>
    private void BuildChoiceButtons()
    {
        if (_choiceContainer == null)
        {
            Debug.LogError("[EventSceneController] _choiceContainer 가 연결되지 않았습니다.", this);
            return;
        }

        if (_choiceButtonPrefab == null)
        {
            Debug.LogError("[EventSceneController] _choiceButtonPrefab 이 연결되지 않았습니다.", this);
            return;
        }

        // 이전 호출로 생성된 버튼이 남아있을 수 있으므로 기존 자식을 모두 제거한다
        foreach (Transform child in _choiceContainer)
            Destroy(child.gameObject);

        foreach (EventChoice choice in _choices)
        {
            EventChoiceButton button = Object.Instantiate(_choiceButtonPrefab, _choiceContainer);
            // 각 버튼에 개별 choice 데이터를 바인딩하기 위해 람다를 사용한다
            button.Setup(choice, () => OnChoiceSelected(choice));
        }
    }

    /// <summary>
    /// 플레이어가 선택지를 선택했을 때 호출된다.
    /// EffectType 이 Battle 이면 전투 씬 전환을 요청하고,
    /// 그 외에는 IEventEffectApplier 를 통해 효과를 적용한 뒤 스테이지를 완료한다.
    /// </summary>
    /// <param name="choice">선택된 EventChoice 데이터.</param>
    private void OnChoiceSelected(EventChoice choice)
    {
        // 중복 선택 방지 — 모든 버튼을 비활성화한다
        SetButtonsInteractable(false);

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
            SetButtonsInteractable(true);
            return;
        }

        IReadOnlyList<MonsterGroupData> pool = _battleConfig.MonsterGroupPool;

        if (pool == null || pool.Count == 0)
        {
            Debug.LogError("[EventSceneController] MonsterGroupPool 이 비어 있습니다. 전투 전환을 취소합니다.");
            SetButtonsInteractable(true);
            return;
        }

        // 몬스터 풀에서 랜덤으로 그룹 하나를 선택한다
        int randomIndex = UnityEngine.Random.Range(0, pool.Count);
        MonsterGroupData selectedGroup = pool[randomIndex];

        // 선택된 그룹을 EventBattleContext 에 기록한다
        // StageLoader.TransitionToEventBattleAsync() 에서 NormalStageContext 로 이전된다
        EventBattleContext.Set(selectedGroup.Id, selectedGroup.MonsterIds);

        StageResult result = new StageResult
        {
            IsSuccess = true,
            IsGameOver = false,
            IsBattleTriggered = true,
        };

        StageCompletionChannel.Invoke(result);
    }

    /// <summary>
    /// _choiceContainer 의 모든 EventChoiceButton 의 인터랙션 가능 여부를 설정한다.
    /// </summary>
    /// <param name="interactable">true 면 활성화, false 면 비활성화.</param>
    private void SetButtonsInteractable(bool interactable)
    {
        if (_choiceContainer == null)
            return;

        foreach (Transform child in _choiceContainer)
        {
            EventChoiceButton btn = child.GetComponent<EventChoiceButton>();
            if (btn != null)
                btn.SetInteractable(interactable);
        }
    }
}
