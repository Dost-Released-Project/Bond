using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

/// <summary>
/// IEventChoiceView 의 UI Toolkit 구현체.
/// JournalUIView 의 SetOptions() 패턴을 참고해 VisualTreeAsset 인스턴스화 방식으로 구현한다.
/// EventSceneController 에 있던 BuildChoiceButtons(), SetButtonsInteractable() 로직을 이전한다.
///
/// [RequireComponent] UIDocument — UXML 문서를 로드하는 컴포넌트.
///
/// Inspector 연결 필요:
///   _choiceButtonTemplate — EventChoiceButton.uxml (VisualTreeAsset)
/// UIDocument 의 Source Asset 에는 EventScene.uxml 을 연결한다.
/// </summary>
[RequireComponent(typeof(UIDocument))]
public class EventSceneView : MonoBehaviour, IEventChoiceView
{
    [SerializeField] private VisualTreeAsset _choiceButtonTemplate;

    private VisualElement _choiceContainer;

    /// <summary>플레이어가 선택지를 클릭했을 때 EventChoicePresenter 가 구독하는 핸들러.</summary>
    public Action<EventChoice> OnChoiceSelected { get; set; }

    private void OnEnable()
    {
        UIDocument uiDocument = GetComponent<UIDocument>();
        VisualElement root = uiDocument.rootVisualElement;

        if (root == null)
        {
            Debug.LogError("[EventSceneView] UIDocument 의 rootVisualElement 가 null 입니다.", this);
            return;
        }

        // UXML 에 정의된 Name(#)으로 선택지 컨테이너를 쿼리한다 (JournalUIView 패턴 동일)
        _choiceContainer = root.Q<VisualElement>("ChoiceContainer");

        if (_choiceContainer == null)
        {
            Debug.LogError("[EventSceneView] UXML 에서 'ChoiceContainer' 를 찾을 수 없습니다.", this);
        }
    }

    /// <summary>
    /// 선택지 목록을 받아 UXML 템플릿 기반으로 버튼을 동적 생성한다.
    /// JournalUIView.SetOptions() 와 동일한 VisualTreeAsset.Instantiate() 패턴을 사용한다.
    /// </summary>
    /// <param name="choices">표시할 선택지 목록.</param>
    public void ShowChoices(IReadOnlyList<EventChoice> choices)
    {
        if (_choiceContainer == null)
        {
            Debug.LogError("[EventSceneView] _choiceContainer 가 초기화되지 않았습니다.", this);
            return;
        }

        if (_choiceButtonTemplate == null)
        {
            Debug.LogError("[EventSceneView] _choiceButtonTemplate 이 연결되지 않았습니다.", this);
            return;
        }

        ClearChoices();

        foreach (EventChoice choice in choices)
        {
            // JournalUIView.SetOptions() 와 동일한 방식: VisualTreeAsset.Instantiate()
            TemplateContainer buttonInstance = _choiceButtonTemplate.Instantiate();
            Button btn = buttonInstance.Q<Button>("ChoiceButton");

            if (btn == null)
            {
                Debug.LogWarning("[EventSceneView] 템플릿에서 'ChoiceButton' 을 찾을 수 없습니다.");
                continue;
            }

            Label label = btn.Q<Label>("ChoiceLabel");
            if (label != null)
            {
                label.text = choice.Label;
            }

            // 각 버튼에 개별 choice 데이터를 바인딩하기 위해 람다를 사용한다
            EventChoice captured = choice;
            btn.clicked += () => OnChoiceSelected?.Invoke(captured);

            _choiceContainer.Add(buttonInstance);
        }
    }

    /// <summary>
    /// 모든 선택지 버튼의 인터랙션 가능 여부를 설정한다.
    /// UI Toolkit 은 SetEnabled() 로 인터랙션 가능 여부를 제어한다 (JournalUIView.SetPrevButtonEnabled 패턴 동일).
    /// </summary>
    /// <param name="interactable">true 면 활성화, false 면 비활성화.</param>
    public void SetInteractable(bool interactable)
    {
        if (_choiceContainer == null)
        {
            return;
        }

        foreach (VisualElement child in _choiceContainer.Children())
        {
            Button btn = child.Q<Button>("ChoiceButton");
            if (btn != null)
            {
                btn.SetEnabled(interactable);
            }
        }
    }

    /// <summary>
    /// 선택지 컨테이너의 모든 자식 요소를 제거한다.
    /// JournalUIView.ClearUI() 의 _optionButtonContainer.Clear() 패턴과 동일.
    /// </summary>
    public void ClearChoices()
    {
        if (_choiceContainer == null)
        {
            return;
        }

        _choiceContainer.Clear();
    }
}
