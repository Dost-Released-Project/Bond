using Bond.Tutorial;
using Bond.WT.Journal;
using UnityEngine;
using UnityEngine.Serialization;
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
///   - RegisterJournalUI() 추가 — 탐사 종료 후 일지 팝업 UI 표시
///
/// Inspector 연결 필요:
///   _mapUIController — 씬에 배치된 MapUIController MonoBehaviour
///   _journalUIPrefab — JournalUIView 프리팹 (null 이면 씬 내 인스턴스를 자동 탐색)
/// </summary>
public class TestMapLifetimeScope : LifetimeScope
{
    [SerializeField] private MapUIController mapUIController;
    [SerializeField] private JournalUIView _journalUIPrefab;

    protected override void Configure(IContainerBuilder builder)
    {
        // IMapGenerator 를 FixedMapGenerator 로 교체 — 변경점
        builder.Register<IMapGenerator, FixedMapGenerator>(Lifetime.Singleton);

        // 이하 MapLifetimeScope 와 동일
        builder.Register<IMapRepository, MapRepository>(Lifetime.Singleton);
        builder.Register<IMapNavigator, MapNavigator>(Lifetime.Singleton);
        builder.Register<IEventEffectApplier, EventEffectApplier>(Lifetime.Singleton);

        // IEventEffectHandler 구현체 등록 — AsImplementedInterfaces() 로 IReadOnlyList<IEventEffectHandler> 자동 주입
        builder.Register<HpChangeEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<ItemRewardEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<StatusEffectEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();
        builder.Register<BattleEventEffectHandler>(Lifetime.Singleton).AsImplementedInterfaces();

        // 씬에 배치된 MonoBehaviour 를 DI 대상으로 등록
        if (mapUIController != null)
            builder.RegisterComponent(mapUIController);
        else
            Debug.LogError("[TestMapLifetimeScope] _mapUIController 가 연결되지 않았습니다.", this);

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

        // 탐사 일지 팝업 UI 등록 — 탐사 종료 후 일지를 표시한다
        // Inspector 에서 _journalUIPrefab 슬롯에 JournalUIView 프리팹 연결 필요
        // 프리팹이 null 이면 JournalScopeExtensions 내부에서 씬 내 인스턴스를 자동 탐색
        builder.RegisterJournalUI(_journalUIPrefab);
        
        // 튜토리얼
        // 순수 C# 코어 컨트롤러 등록 (이미 등록되어 있으므로 구조 인지용)
        builder.Register<ExpeditionTutorialSystemController>(Lifetime.Singleton);
        builder.Register<ExpeditionTutorialEntryPoint>(Lifetime.Singleton).AsImplementedInterfaces();
        // 씬에 배치된 UI Toolkit 마스킹 뷰 등록 파트
        builder.RegisterComponentInHierarchy<TutorialExpeditionView>();
    }
}
