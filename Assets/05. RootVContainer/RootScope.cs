using Bond.Expedition;
using Bond.WT.Journal;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace RootVContainer
{
    public class RootScope : LifetimeScope
    {
        [SerializeField] private MapConfigLoaderSettings _mapConfigLoaderSettings;

        // [SerializeField] private JournalUIView _journalUIView; // 동적 바인딩으로 변경

        protected override void Configure(IContainerBuilder builder)
        {
            builder.Register<ExpeditionPayload>(Lifetime.Singleton);

            // MapConfigLoaderSettings — Inspector 에서 에셋을 연결한다.
            if (_mapConfigLoaderSettings != null)
                builder.RegisterInstance(_mapConfigLoaderSettings);
            else
                Debug.LogError("[RootScope] _mapConfigLoaderSettings 가 연결되지 않았습니다.", this);

            // IMapConfigLoader → MapConfigLoader (Singleton)
            // RootScope Singleton: 씬 전환 후에도 동일 인스턴스를 재사용해 핸들 누수를 방지한다.
            builder.Register<IMapConfigLoader, MapConfigLoader>(Lifetime.Singleton);

            // MapConfigCache (Singleton)
            // MapInitializer 가 SO 해제 전에 StageLoader 참조용 데이터를 보관하는 캐시.
            builder.Register<MapConfigCache>(Lifetime.Singleton);

            // ISpriteLoader → SpriteLoader (Singleton)
            // Addressables Sprite 비동기 로드 공용 서비스.
            // RootScope 등록: TurnLifetimeScope, MapLifetimeScope 등 모든 하위 스코프에서 주입 가능.
            builder.Register<ISpriteLoader, SpriteLoader>(Lifetime.Singleton);

            // =================================================================================
            // [ Journal System ] - 일지 시스템 관련 컴포넌트 등록
            // =================================================================================
            
            // View Instance 동적 탐색 (비활성화된 오브젝트 포함)
            var journalView = UnityEngine.Object.FindAnyObjectByType<JournalUIView>(FindObjectsInactive.Include);
            if (journalView != null)
                builder.RegisterComponent(journalView).AsImplementedInterfaces();
            else
                Debug.LogWarning("[RootScope] 씬에서 JournalUIView를 찾을 수 없습니다. UI 컴포넌트를 씬 캔버스에 배치해주세요.", this);
            
            // DataBaseSO 로드 및 등록 (Addressables 동기 로드 방식)
            var journalDBHandle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<JournalDataBaseSO>("JournalDataBase");
            var journalDB = journalDBHandle.WaitForCompletion();
            if (journalDB != null)
                builder.RegisterInstance(journalDB);
            else
                Debug.LogError("[RootScope] JournalDataBase 를 로드할 수 없습니다. Addressables Group을 확인하세요.", this);

            // Data & Logic
            builder.Register<JournalModel>(Lifetime.Singleton);
            builder.Register<LocationEventProvider>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MockJournalActionHandler>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterEntryPoint<JournalSystem>(Lifetime.Singleton).AsSelf();
            builder.RegisterEntryPoint<JournalBinder>(Lifetime.Singleton);
            
            // [Test] 런타임 테스트 러너
            builder.RegisterEntryPoint<JournalTestRunner>(Lifetime.Singleton).AsSelf();

            // =================================================================================
        }
    }
}