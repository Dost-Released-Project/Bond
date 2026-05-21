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

        // 이벤트 씬 생명주기 동안만 JournalSystem 에 등록/해제되는 Provider
        // AsSelf(): EventSceneController 가 구체 타입으로 직접 주입받아 RecordChoice() 를 호출하기 위해 노출
        builder.RegisterEntryPoint<EventJournalProvider>(Lifetime.Scoped).AsSelf();

        builder.RegisterComponentInHierarchy<EventSceneView>()
            .AsImplementedInterfaces()
            .AsSelf();
        builder.RegisterEntryPoint<EventChoicePresenter>(Lifetime.Scoped);
    }
}
