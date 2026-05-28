using Bond.Expedition;
using Bond.WT.Journal;
using UnityEngine;
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
    [SerializeField] private FakeEventContext _fakeEventContext;

    protected override void Configure(IContainerBuilder builder)
    {
        // IEventContext → FakeEventContext (Inspector SO 직접 지정)
        if (_fakeEventContext != null)
        {
            builder.RegisterComponent(_fakeEventContext).As<IEventContext>();
        }
        else
        {
            Debug.LogError("[EventTestScope] _fakeEventContext 가 연결되지 않았습니다.", this);
        }

        // IEventEffectApplier → NullEventEffectApplier (효과 미적용 스텁)
        builder.Register<NullEventEffectApplier>(Lifetime.Singleton).As<IEventEffectApplier>();

        // NullJournalSystem 을 JournalSystem 타입으로도 노출 → EventJournalProvider 생성자 충족
        builder.Register<NullJournalSystem>(Lifetime.Singleton).As<JournalSystem>().AsSelf();

        // JournalDataBaseSO Mock 등록 불필요 — EventJournalProvider 가 DB 를 참조하지 않음
        // EventData SO 의 _journalData 슬롯에 직접 연결된 JournalDataSO 를 사용한다

        // ExpeditionPayload — 테스트 씬에서 RootScope 없이 단독 실행 시 필요
        // JournalInventoryActionHandler 가 생성자에서 주입받는다
        builder.Register<ExpeditionPayload>(Lifetime.Singleton);

        // 2차 선택지 actionKey 처리 핸들러 등록
        // AsImplementedInterfaces(): IReadOnlyList<IJournalActionHandler> 로 자동 수집된다
        builder.Register<JournalInventoryActionHandler>(Lifetime.Scoped)
            .AsImplementedInterfaces();

        // EventSceneController (씬 히어라키에서 탐색)
        builder.RegisterComponentInHierarchy<EventSceneController>();

        builder.RegisterComponentInHierarchy<EventSceneView>()
            .AsImplementedInterfaces()
            .AsSelf();
        builder.RegisterEntryPoint<EventChoicePresenter>(Lifetime.Scoped);
    }
}
