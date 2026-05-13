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

        [Header("Journal System")]
        [SerializeField] private JournalUIView _journalUIView;

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
            
            // View Instance (IJournalVisualizer 인터페이스로 등록)
            // if (_journalUIView != null)
            //     builder.RegisterComponent(_journalUIView).AsImplementedInterfaces();
            // else
            //     Debug.LogWarning("[RootScope] _journalUIView 가 연결되지 않았습니다. 일지 시스템이 정상 작동하지 않을 수 있습니다.", this);
            //
            // // Data & Logic
            // builder.Register<JournalModel>(Lifetime.Singleton);
            // builder.RegisterEntryPoint<JournalSystem>(Lifetime.Singleton);
            // builder.RegisterEntryPoint<JournalBinder>(Lifetime.Singleton);

            // =================================================================================
        }
    }
}