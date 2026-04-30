using System.Collections.Generic;
using UnityEngine;

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

    private void Start()
    {
        // 씬 로드 직후 컨텍스트를 읽고 즉시 Clear() 호출
        // 방어적 복사 — Clear() 이후에도 로컬 참조가 유효하도록
        _choices = new List<EventChoice>(EventContext.Choices);
        EventContext.Clear();

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
    /// HpChange 적용, RewardIds 처리 후 StageCompletionChannel.Invoke() 로 씬을 닫는다.
    /// </summary>
    /// <param name="choice">선택된 EventChoice 데이터.</param>
    private void OnChoiceSelected(EventChoice choice)
    {
        // TODO: choice.HpChange 를 PlayerStatus 에 적용
        // TODO: choice.RewardIds 를 RewardSystem 에 전달

        StageResult result = new StageResult
        {
            IsSuccess = true,
            IsGameOver = false,
            RewardIds = new List<string>(choice.RewardIds),
        };

        StageCompletionChannel.Invoke(result);

        Debug.Log($"[EventSceneController] 선택 완료: {choice.Label}, IsSuccess={result.IsSuccess}");
    }
}
