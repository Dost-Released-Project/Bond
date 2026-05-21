using Bond.WT.Journal;
using VContainer;
using VContainer.Unity;

/// <summary>
/// EventTestScene 전용 VContainer 스코프.
/// RootScope / MapLifetimeScope 없이 단독으로 이벤트 씬을 실행하기 위한
/// Mock 등록 스코프다.
///
/// Inspector 설정 필요:
///   Parent            — 없음 (None 으로 설정)
///   _fakeEventContext — 씬에 배치된 FakeEventContext MonoBehaviour
/// </summary>
public class EventTestScope : LifetimeScope
{
    [UnityEngine.SerializeField] private FakeEventContext _fakeEventContext;

    protected override void Configure(IContainerBuilder builder)
    {
        // IEventContext → FakeEventContext (Inspector SO 직접 지정)
        if (_fakeEventContext != null)
        {
            builder.RegisterComponent(_fakeEventContext).As<IEventContext>();
        }
        else
        {
            UnityEngine.Debug.LogError("[EventTestScope] _fakeEventContext 가 연결되지 않았습니다.", this);
        }

        // IEventEffectApplier → NullEventEffectApplier (효과 미적용 스텁)
        builder.Register<NullEventEffectApplier>(Lifetime.Singleton).As<IEventEffectApplier>();

        // NullJournalSystem 을 JournalSystem 타입으로도 노출 → EventJournalProvider 생성자 충족
        builder.Register<NullJournalSystem>(Lifetime.Singleton).As<JournalSystem>().AsSelf();

        // EventJournalProvider — JournalSystem 대신 NullJournalSystem 주입
        // AsSelf(): EventSceneController 가 구체 타입으로 직접 주입받아 RecordChoice() 를 호출하기 위해 노출
        builder.RegisterEntryPoint<EventJournalProvider>(Lifetime.Scoped).AsSelf();

        // EventSceneController (씬 히어라키에서 탐색)
        builder.RegisterComponentInHierarchy<EventSceneController>();
    }
}
