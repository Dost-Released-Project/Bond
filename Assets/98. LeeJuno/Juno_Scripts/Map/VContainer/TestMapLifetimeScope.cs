using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 3노드 고정 맵 테스트용 LifetimeScope.
/// MapLifetimeScope 와 동일하되, IMapGenerator 바인딩을 FixedMapGenerator 로 교체한다.
/// 테스트 씬의 LifetimeScope 컴포넌트를 이 클래스로 교체하면
/// 나머지 맵 파이프라인(MapInitializer, MapNavigator, StageLoader 등)은 그대로 동작한다.
///
/// 변경점:
///   - IMapGenerator -> FixedMapGenerator (Normal->Event->Camping 3노드 고정 맵 반환)
///
/// Inspector 연결 필요:
///   _mapUIController -- 씬에 배치된 MapUIController MonoBehaviour
/// </summary>
public class TestMapLifetimeScope : LifetimeScope
{
    [SerializeField] private MapUIController _mapUIController;

    protected override void Configure(IContainerBuilder builder)
    {
        // IMapGenerator 를 FixedMapGenerator 로 교체 -- 변경점
        builder.Register<IMapGenerator, FixedMapGenerator>(Lifetime.Singleton);

        // 이하 MapLifetimeScope 와 동일
        builder.Register<IMapRepository, MapRepository>(Lifetime.Singleton);
        builder.Register<IMapNavigator, MapNavigator>(Lifetime.Singleton);
        builder.Register<IStageLoader, StageLoader>(Lifetime.Singleton);
        builder.Register<IEventEffectApplier, EventEffectApplier>(Lifetime.Singleton);

        // IEventEffectHandler 구현체 등록 -- AsImplementedInterfaces() 로 IReadOnlyList<IEventEffectHandler> 자동 주입
        builder.Register<HpChangeEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<ItemRewardEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<StatusEffectEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<BattleEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();

        // IEventContext -> EventContextService (Singleton)
        builder.Register<IEventContext, EventContextService>(Lifetime.Singleton);

        // 씬에 배치된 MonoBehaviour 를 DI 대상으로 등록
        if (_mapUIController != null)
            builder.RegisterComponent(_mapUIController);
        else
            Debug.LogError("[TestMapLifetimeScope] _mapUIController 가 연결되지 않았습니다.", this);

        // Config 로드 + 맵 생성 담당 EntryPoint
        builder.RegisterEntryPoint<MapInitializer>();
    }
}
