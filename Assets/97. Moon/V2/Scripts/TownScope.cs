using System;
using Bond.Embark;
using Bond.Expedition;
using Bond.Tutorial;
using Bond.UI;
using Bond.UI.Town;
using Bond.WT.Journal;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VContainer;
using VContainer.Unity;

public class TownScope : LifetimeScope
{
    [SerializeField] private Bond.WT.Journal.JournalUIView _journalUIPrefab;

    protected override void Configure(IContainerBuilder builder)
    {
        // Manager & Service (싱글톤처럼 유지)
        // 인벤토리
        var payload = Parent.Container.Resolve<ExpeditionPayload>();
        var skillDb = DBSORegistry.LoadSync<SkillDataBaseSO>("SkillDataBase");
    
        builder.RegisterInstance(payload.Supplies).AsImplementedInterfaces().AsSelf();
        builder.Register<TotalInventory>(Lifetime.Scoped).WithParameter("capacity", 16).AsImplementedInterfaces().AsSelf();
        builder.Register<InventoryTransferService>(Lifetime.Singleton);
        builder.Register<CharacterItemService>(Lifetime.Singleton);
        builder.Register<ExpeditionResultService>(Lifetime.Singleton);
        // 자원 및 건물
        builder.Register<ResourceManager>(Lifetime.Singleton);
        builder.Register<BuildingService>(Lifetime.Singleton);

        // 캐릭터 & 출정
        builder.Register<IExpeditionRegionProvider, TestExpeditionRegionProvider>(Lifetime.Scoped);
        builder.Register<EmbarkController>(Lifetime.Scoped);
        //builder.Register<PartyController>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        builder.Register<StageCoach>(Lifetime.Scoped);
        // Roster 는 RootScope Singleton 으로 승격(ConfigureRoster) — 씬 전환 간 인스턴스 유지.
        builder.Register<CharacterSelector>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
        builder.Register<CharacterDetailController>(Lifetime.Scoped).WithParameter("skillDb", skillDb);
        builder.RegisterComponentInHierarchy<CharacterDetailPresenter>();
        //builder.RegisterComponentInHierarchy<CharacterCombatPanelPresenter>();

        // 씬에 배치된 컴포넌트
        builder.RegisterComponentInHierarchy<TownUIController>();
        builder.RegisterComponentInHierarchy<InventoryView>();
        builder.RegisterComponentInHierarchy<ResourceView>();
        builder.RegisterComponentInHierarchy<SupplyView>();
        builder.RegisterComponentInHierarchy<SettlementManager>();
        builder.RegisterComponentInHierarchy<ConstructionUI>();
        builder.RegisterComponentInHierarchy<InteractionManager>();
        builder.RegisterComponentInHierarchy<SupplyManager>();
        //builder.RegisterComponentInHierarchy<ExpeditionInventoryView>();
        builder.RegisterComponentInHierarchy<AccessoryBagView>();
        builder.RegisterComponentInHierarchy<SmithyUIController>();
        
        // 순수 C# 코어 컨트롤러 등록 (프로젝트 스코프에 이미 Singleton으로 등록되어 있다면 제외 가능)
        builder.Register<TutorialSystemController>(Lifetime.Singleton);
        // 어드레서블 로드 및 컨트롤러 초기화 바인딩을 담당할 엔트리 포인트 등록
        builder.Register<TutorialEntryPoint>(Lifetime.Singleton).AsImplementedInterfaces();
        // 씬에 배치된 UI Toolkit 마스킹 뷰 등록
        builder.RegisterComponentInHierarchy<TutorialTownView>();

#if UNITY_EDITOR
        // 테스트용 스크립트
        builder.RegisterComponentInHierarchy<_90._HA.Temp.Test.S1Test>();
#endif
        
        // Journal UI 및 Binder 지역 스코프 등록
        builder.RegisterJournalUI(_journalUIPrefab);
    }
}