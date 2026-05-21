using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 맵 시스템의 VContainer 의존성 등록 스코프.
/// 이 씬에 올라가 있으면 맵 시스템 전체가 DI 컨테이너로 관리된다.
///
/// 변경 사항:
///   - Inspector SO 4종 제거 (Addressables 로드로 대체)
///   - MapInitializer EntryPoint 추가 (Config 로드 + 맵 생성 담당)
///   - IMapConfigLoader 와 MapConfigCache 는 RootScope(부모)에서 상속 해결된다.
///   - StageLoader 는 MapConfigCache 를 주입받아 SO 데이터를 참조한다.
///
/// 등록 목록:
///   - IMapGenerator   → MapGenerator  (Singleton)
///   - IMapRepository  → MapRepository (Singleton)
///   - IMapNavigator   → MapNavigator  (Singleton)
///   - IStageLoader    → StageLoader   (Singleton)
///   - MapUIController : MonoBehaviour 컴포넌트 등록
///   - MapInitializer  : EntryPoint (Config 로드 + 맵 생성)
///
/// Inspector 연결 필요:
///   _mapUIController — 씬에 배치된 MapUIController MonoBehaviour
/// </summary>
public class MapLifetimeScope : LifetimeScope
{
    [SerializeField] private MapUIController _mapUIController;

    protected override void Configure(IContainerBuilder builder)
    {
        // 맵 시스템 핵심 서비스 등록 (인터페이스 → 구현체 바인딩)
        builder.Register<IMapGenerator, MapGenerator>(Lifetime.Singleton);
        builder.Register<IMapRepository, MapRepository>(Lifetime.Singleton);
        builder.Register<IMapNavigator, MapNavigator>(Lifetime.Singleton);
        builder.Register<IStageLoader, StageLoader>(Lifetime.Singleton);
        builder.Register<IEventEffectApplier, EventEffectApplier>(Lifetime.Singleton);

        // IEventEffectHandler 구현체 등록 — AsImplementedInterfaces() 로 IReadOnlyList<IEventEffectHandler> 자동 주입
        // EffectType 은 배타적이므로 각 Handler 는 Singleton 으로 등록한다
        builder.Register<HpChangeEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<ItemRewardEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<StatusEffectEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<BattleEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();

        // IEventContext → EventContextService (Singleton)
        // StageLoader 가 씬 로드 직전에 기록하고, EventSceneController 가 씬 진입 시 읽는 단방향 채널.
        // MapLifetimeScope Singleton: 맵 씬 생명주기 동안 유지. EventSceneLifetimeScope 가 부모를 통해 상속 접근.
        builder.Register<IEventContext, EventContextService>(Lifetime.Singleton);

        // 씬에 배치된 MonoBehaviour 를 DI 대상으로 등록
        if (_mapUIController != null)
            builder.RegisterComponent(_mapUIController);
        else
            Debug.LogError("[MapLifetimeScope] _mapUIController 가 연결되지 않았습니다.", this);
        
        // Config 로드 + 맵 생성 담당 EntryPoint
        builder.RegisterEntryPoint<MapInitializer>();
    }
}