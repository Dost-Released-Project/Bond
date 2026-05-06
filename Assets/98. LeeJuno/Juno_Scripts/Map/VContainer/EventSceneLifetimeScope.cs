using VContainer;
using VContainer.Unity;

/// <summary>
/// 이벤트 씬의 VContainer 의존성 등록 스코프.
/// MapLifetimeScope 를 부모로 설정해 IEventEffectApplier 를 상속받는다.
///
/// Inspector 설정 필요:
///   parentReference -> MapLifetimeScope (에디터에서 수동 연결)
/// </summary>
public class EventSceneLifetimeScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        // 이벤트 씬에 배치된 EventSceneController 를 DI 대상으로 등록한다.
        // MapLifetimeScope 에 등록된 IEventEffectApplier 를 부모 스코프에서 상속받아 주입받는다.
        builder.RegisterComponentInHierarchy<EventSceneController>();
    }
}
