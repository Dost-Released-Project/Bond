using System;
using System.Collections.Generic;
using Bond.WT.Journal;
using VContainer;
using VContainer.Unity;

/// <summary>
/// IEventChoiceView 의 IJournalVisualizer(JournalUIView) 기반 구현체.
/// 이벤트 씬의 1차/2차 선택지를 JournalUIView 팝업 형식으로 표시한다.
///
/// 역할:
///   - ShowDescription()  : JournalUIView 를 열고 이벤트 설명 텍스트를 표시한다
///   - ShowChoices()      : EventChoice 목록을 JournalOption 으로 변환해 선택지 버튼을 생성한다
///   - ShowSecondaryPhase(): 2차 선택지(JournalOption)를 표시한다
///   - Dispose()          : JournalBinder 의 OnOptionSelected 콜백을 복원하고 팝업을 닫는다
///
/// IStartable 을 구현해 Start() 시점에 JournalBinder 가 설정한 OnOptionSelected 를 저장한다.
/// IDisposable 을 구현해 EventSceneLifetimeScope 종료 시 저장한 콜백을 복원한다.
/// </summary>
public class EventJournalChoiceView : IEventChoiceView, IStartable, IDisposable
{
    private readonly IJournalVisualizer _view;

    private readonly List<JournalOption> _primaryOptions = new List<JournalOption>();
    private readonly List<EventChoice> _primaryChoices = new List<EventChoice>();

    /// <summary>
    /// MapLifetimeScope 의 JournalBinder.Start() 가 설정한 OnOptionSelected 콜백.
    /// Dispose 시 복원해 저널 팝업 선택지가 정상 동작하게 한다.
    /// </summary>
    private Action<JournalOption> _savedOnOptionSelected;

    /// <summary>중복 클릭 방지 플래그. SetInteractable() 로 제어한다.</summary>
    private bool _isInteractable = true;

    public Action<EventChoice> OnChoiceSelected { get; set; }
    public Action<JournalOption> OnSecondaryOptionSelected { get; set; }

    [Inject]
    public EventJournalChoiceView(IJournalVisualizer view)
    {
        _view = view;
    }

    /// <summary>
    /// VContainer IStartable.Start() — EventSceneLifetimeScope 초기화 시 호출된다.
    /// MapLifetimeScope 의 JournalBinder.Start() 가 먼저 실행되므로
    /// 이 시점의 _view.OnOptionSelected 에는 JournalBinder 의 콜백이 담겨 있다.
    /// </summary>
    public void Start()
    {
        _savedOnOptionSelected = _view.OnOptionSelected;
    }

    /// <summary>
    /// VContainer IDisposable.Dispose() — EventSceneLifetimeScope 종료 시 호출된다.
    /// JournalBinder 의 OnOptionSelected 콜백을 복원하고 팝업을 닫는다.
    /// </summary>
    public void Dispose()
    {
        _view.OnOptionSelected = _savedOnOptionSelected;
        _view.SetVisible(false);
    }

    /// <summary>
    /// 이벤트 설명 텍스트를 JournalUIView 에 표시한다.
    /// isTyping: false — 타이핑 코루틴 종료 시 NextButton 이 자동 표시되는 것을 방지한다.
    /// </summary>
    public void ShowDescription(string description)
    {
        _view.ClearUI();
        _view.SetVisible(true);
        _view.ShowText(description ?? string.Empty, isTyping: false);
    }

    /// <summary>
    /// 1차 이벤트 선택지를 JournalOption 으로 변환하여 JournalUIView 에 표시한다.
    /// EventChoice.Label 을 JournalOption.text 로 매핑하고 인덱스로 역참조한다.
    /// </summary>
    public void ShowChoices(IReadOnlyList<EventChoice> choices)
    {
        _primaryChoices.Clear();
        _primaryOptions.Clear();
        _isInteractable = true;

        foreach (EventChoice choice in choices)
        {
            _primaryChoices.Add(choice);
            _primaryOptions.Add(new JournalOption { text = choice.Label, actionKey = string.Empty });
        }

        // 람다식: 선택된 JournalOption 을 인덱스로 역참조해 원본 EventChoice 를 OnChoiceSelected 에 전달하기 위해 사용한다
        _view.OnOptionSelected = option =>
        {
            if (_isInteractable == false)
                return;

            int index = _primaryOptions.IndexOf(option);
            if (index >= 0 && index < _primaryChoices.Count)
                OnChoiceSelected?.Invoke(_primaryChoices[index]);
        };

        _view.SetOptions(_primaryOptions);
    }

    /// <summary>
    /// 중복 선택 방지를 위해 인터랙션 가능 여부를 플래그로 제어한다.
    /// JournalUIView 는 버튼별 SetEnabled API 를 제공하지 않으므로 클릭 이벤트를 무시하는 방식으로 구현한다.
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        _isInteractable = interactable;
    }

    /// <summary>선택지 및 UI 를 초기화한다.</summary>
    public void ClearChoices()
    {
        _primaryChoices.Clear();
        _primaryOptions.Clear();
        _view.ClearUI();
    }

    /// <summary>
    /// 2차 선택지 화면으로 전환한다.
    /// paragraphs 를 텍스트로 즉시 표시하고 JournalOption 버튼을 생성한다.
    /// isTyping: false — 타이핑 코루틴 종료 시 NextButton 이 자동 표시되는 것을 방지한다.
    /// </summary>
    public void ShowSecondaryPhase(IReadOnlyList<string> paragraphs, IReadOnlyList<JournalOption> options)
    {
        string text = paragraphs != null ? string.Join("\n\n", paragraphs) : string.Empty;
        _view.ShowText(text, isTyping: false);

        // 람다식: 2차 선택지 클릭 시 OnSecondaryOptionSelected 핸들러에 선택된 JournalOption 을 전달하기 위해 사용한다
        _view.OnOptionSelected = option =>
        {
            if (_isInteractable == false)
                return;

            OnSecondaryOptionSelected?.Invoke(option);
        };

        _view.SetOptions(options);
    }
}
