using Bond.WT.Journal;
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
///   - MapUIController : MonoBehaviour 컴포넌트 등록
///   - MapInitializer  : EntryPoint (Config 로드 + 맵 생성)
///
/// Inspector 연결 필요:
///   _mapUIController — 씬에 배치된 MapUIController MonoBehaviour
/// </summary>
public class MapLifetimeScope : LifetimeScope
{
    [SerializeField] private MapUIController mapUIController;
    [SerializeField] private JournalUIView _journalUIPrefab;

    protected override void Configure(IContainerBuilder builder)
    {
        // 맵 시스템 핵심 서비스 등록 (인터페이스 → 구현체 바인딩)
        builder.Register<IMapGenerator, MapGenerator>(Lifetime.Singleton);
        builder.Register<IMapRepository, MapRepository>(Lifetime.Singleton);
        builder.Register<IMapNavigator, MapNavigator>(Lifetime.Singleton);
        builder.Register<IEventEffectApplier, EventEffectApplier>(Lifetime.Singleton);

        // IEventEffectHandler 구현체 등록 — AsImplementedInterfaces() 로 IReadOnlyList<IEventEffectHandler> 자동 주입
        // EffectType 은 배타적이므로 각 Handler 는 Singleton 으로 등록한다
        builder.Register<HpChangeEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();

        // MapConfigCache 를 통해 AccessoryDB 를 주입받으므로 WithParameter 없이 단순 등록한다
        builder.Register<ItemRewardEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();

        builder.Register<StatusEffectEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<BattleEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();

        // 씬에 배치된 MonoBehaviour 를 DI 대상으로 등록
        if (mapUIController != null)
            builder.RegisterComponent(mapUIController);
        else
            Debug.LogError("[MapLifetimeScope] _mapUIController 가 연결되지 않았습니다.", this);
        
        // 런 전체 이벤트 이력 누적 저장소
        builder.Register<EventLogAccumulator>(Lifetime.Singleton);

        // 스킬 이펙트 풀 — 맵씬 Singleton으로 등록하여 전투씬 BattleScope에서 부모 스코프를 통해 주입받는다
        builder.Register<ISkillEffectPool, SkillEffectPool>(Lifetime.Singleton);

        // StageLoader: 맵 스코프에서 등록해야 LifetimeScope 주입 시 맵 스코프가 전달된다
        // EnqueueParent 패턴으로 이벤트/전투 씬 로드 시 올바른 부모 스코프를 지정하기 위해 필요하다
        // WithParameter: LifetimeScope는 VContainer가 자동 주입을 보장하지 않으므로 this(현재 맵 스코프)를 직접 전달한다
        builder.Register<IStageLoader, StageLoader>(Lifetime.Singleton)
            .WithParameter<LifetimeScope>(this);

        // Config 로드 + 맵 생성 담당 EntryPoint
        builder.RegisterEntryPoint<MapInitializer>();

        // 탐사 일지 팝업 UI 등록 — 맵 씬에서 탐사 종료 후 일지를 표시한다
        // Inspector에서 _journalUIPrefab 슬롯에 JournalUIView 프리팹 연결 필요
        // 프리팹이 null이면 JournalScopeExtensions 내부에서 씬 내 인스턴스를 자동 탐색
        builder.RegisterJournalUI(_journalUIPrefab);
    }
}