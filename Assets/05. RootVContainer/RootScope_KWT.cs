using Bond.Expedition;
using Bond.WT.Journal;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace RootVContainer
{
    public partial class RootScope : LifetimeScope
    {
        protected void ConfigureJournal(IContainerBuilder builder)
        {
            #region 일지
            // DataBaseSO 로드 및 등록 (Addressables 동기 로드 방식)
            var journalDBHandle = UnityEngine.AddressableAssets.Addressables.LoadAssetAsync<JournalDataBaseSO>("JournalDataBase");
            var journalDB = journalDBHandle.WaitForCompletion();
            if (journalDB != null)
                builder.RegisterInstance(journalDB);
            else
                Debug.LogError("[RootScope] JournalDataBase 를 로드할 수 없습니다. Addressables Group을 확인하세요.", this);

            // Data & Logic (Global Core)
            builder.Register<JournalModel>(Lifetime.Singleton);
            builder.Register<LocationEventProvider>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            builder.Register<MockJournalActionHandler>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.Register<JournalInventoryActionHandler>(Lifetime.Singleton).AsImplementedInterfaces();
            builder.RegisterEntryPoint<JournalSystem>(Lifetime.Singleton).AsSelf();
            
            // [Test] 런타임 테스트 러너
            builder.RegisterEntryPoint<JournalTestRunner>(Lifetime.Singleton).AsSelf();
            #endregion
        }
    }
}