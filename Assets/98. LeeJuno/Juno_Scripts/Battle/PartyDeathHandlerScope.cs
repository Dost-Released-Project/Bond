using VContainer;
using VContainer.Unity;

/// <summary>
/// PartyDeathHandler를 VContainer에 등록하는 LifetimeScope.
/// 맵 씬 프리팹 계층에 이 컴포넌트를 추가하고
/// Parent를 MapLifetimeScope가 붙은 GameObject로 설정한다.
/// ExpeditionPayload는 RootScope Singleton이므로 상위 스코프에서 자동으로 해소된다.
/// IStageLoader는 MapLifetimeScope Singleton이므로 Parent 체인에서 자동으로 해소된다.
/// </summary>
public class PartyDeathHandlerScope : LifetimeScope
{
    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterEntryPoint<PartyDeathHandler>(Lifetime.Singleton);
    }
}
