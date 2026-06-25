using Bond.WT.Journal;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 이벤트 씬의 VContainer 의존성 등록 스코프.
/// MapLifetimeScope 를 부모로 설정해 IEventEffectApplier, JournalSystem 을 상속받는다.
///
/// Inspector 설정 필요:
///   parentReference -> MapLifetimeScope (에디터에서 수동 연결)
/// </summary>
public class EventSceneLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterComponentInHierarchy<EventSceneController>();

        // EventSceneView 대신 JournalUIView 팝업을 사용하는 어댑터 등록
        // IJournalVisualizer 는 MapLifetimeScope(부모)에서 상속된다
        builder.RegisterEntryPoint<EventJournalChoiceView>(Lifetime.Scoped)
            .AsImplementedInterfaces();
        builder.RegisterEntryPoint<EventChoicePresenter>(Lifetime.Scoped);

    }
}
