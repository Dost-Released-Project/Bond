using System;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 이벤트 선택지의 View 이벤트와 Controller 로직을 연결하는 Presenter.
///
/// 역할:
///   - IEventChoiceView.OnChoiceSelected → EventSceneController.OnChoiceSelectedFromView 연결
///   - View 초기화 및 생명주기 관리
///
/// VContainer IStartable 로 등록되어 Start() 에서 View 이벤트를 Controller 에 연결한다.
/// </summary>
public class EventChoicePresenter : IStartable, IDisposable
{
    private readonly IEventChoiceView _view;
    private readonly EventSceneController _controller;

    /// <summary>
    /// VContainer 생성자 주입.
    /// </summary>
    /// <param name="view">이벤트 선택지 View (UI Toolkit 구현체 또는 다른 구현체).</param>
    /// <param name="controller">이벤트 씬 진입점 Controller.</param>
    [Inject]
    public EventChoicePresenter(IEventChoiceView view, EventSceneController controller)
    {
        _view       = view;
        _controller = controller;
    }

    /// <summary>
    /// VContainer IStartable.Start() — MonoBehaviour 보다 늦게 실행될 수 있다.
    /// BindView() 내부에서 _choices 가 이미 채워진 경우를 방어적으로 처리하므로
    /// Controller.Start() 와 Presenter.Start() 의 순서에 무관하게 동작한다.
    /// </summary>
    public void Start()
    {
        // View 이벤트를 Controller 에 연결한다
        // 람다식: View 와 Controller 를 직접 결합하지 않고 Presenter 가 중간에서 연결하기 위해 사용한다
        _view.OnChoiceSelected = choice => _controller.OnChoiceSelectedFromView(choice);

        // Controller 에 View 를 주입해 초기 선택지를 표시한다
        _controller.BindView(_view);
    }

    /// <summary>
    /// VContainer IDisposable.Dispose() — 스코프 종료 시 View 이벤트 핸들러를 정리한다.
    /// </summary>
    public void Dispose()
    {
        _view.OnChoiceSelected          = null;
        _view.OnSecondaryOptionSelected = null;
    }
}
