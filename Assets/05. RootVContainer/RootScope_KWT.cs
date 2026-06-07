using Bond.WT.Journal;
using Bond.WT.Journal.Handlers;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace RootVContainer
{
    // KWT 파트 전용 RootScope 확장
    public partial class RootScope : LifetimeScope
    {
        private void ConfigureJournal(IContainerBuilder builder)
        {
            #region 일지
            // DataBaseSO 로드 및 등록 (RootScope는 Start 이전 앱 초기화 단계에 실행되므로 동기 로드 필요)
            // LoadSync를 사용하면 내부적으로 WaitForCompletion을 안전하게 호출하며, 이후 비동기 프리로드 시에는 캐시를 재사용합니다.
            var journalDB = DBSORegistry.LoadSync<JournalDataBaseSO>("JournalDataBase");
            if (journalDB != null)
                builder.RegisterInstance(journalDB);
            else
                Debug.LogError("[RootScope] JournalDataBase 를 로드할 수 없습니다. Addressables Group 라벨 및 키를 확인하세요.", this);

            // Data & Logic (Global Core)
            builder.Register<JournalModel>(Lifetime.Singleton);
            builder.Register<LocationEventProvider>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            // builder.Register<MockJournalActionHandler>(Lifetime.Singleton).AsImplementedInterfaces(); // 테스트용 Mock 핸들러가 실 동작을 가로채지 않도록 비활성화
            builder.Register<JournalInventoryActionHandler>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<BattleResultJournalHandler>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterEntryPoint<JournalSystem>(Lifetime.Singleton).AsSelf();

            // [Test] 런타임 테스트 러너
            builder.RegisterEntryPoint<JournalTestRunner>(Lifetime.Singleton).AsSelf();
            #endregion
        }
    }
}