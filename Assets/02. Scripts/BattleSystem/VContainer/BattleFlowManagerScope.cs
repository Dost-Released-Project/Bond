using BattleStage;
using BattleSystem.Interface;
using Bond.UI;
using Bond.WT.Journal;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace BattleSystem.VContainer
{
    public class BattleFlowManagerScope : LifetimeScope
    {
        [SerializeField] private BattleFlowManager expeditionFlowManager;
        [SerializeField] private Bond.WT.Journal.JournalUIView _journalUIPrefab;
        [SerializeField] private BattleResultView _battleResultView;
        
        public CharacterSlot[] slots;
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterComponent(expeditionFlowManager).As<IBattleFlowManager>();
            builder.Register<FormationManager>(Lifetime.Singleton).As<IFormationManager>();
            builder.RegisterInstance(slots);
            
            // 캐릭터 아이템 사용 및 장착 서비스
            builder.Register<CharacterItemService>(Lifetime.Singleton);
            // 아이템 이동 서비스
            builder.Register<InventoryTransferService>(Lifetime.Singleton);
            // 캐릭터 선택 지정
            builder.Register<CharacterSelector>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();
            
            // 퇴각(도주) 컨트롤러 및 뷰
            builder.Register<BattleRetreatController>(Lifetime.Scoped);
            builder.Register<BattleRetreatJournalHandler>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();
            builder.RegisterComponentInHierarchy<BattleRetreatView>();
            
            // 탐사 인벤토리 UI
            builder.RegisterComponentInHierarchy<ExpeditionInventoryView>();
            // 캐릭터 슬롯과 캐릭터 셀렉터 연결용
            builder.RegisterComponentInHierarchy<BattleFormationPresenter>();

            // Journal 연동 Provider (스코프 시작 시 JournalSystem에 자가 등록)
            builder.RegisterEntryPoint<Bond.WT.Journal.BattleEventProvider>(Lifetime.Singleton).AsSelf();

            // Journal UI 및 Binder 지역 스코프 등록
            builder.RegisterJournalUI(_journalUIPrefab);

            // 신규 전투 결과 UI 및 프레젠터 등록
            if (_battleResultView != null)
            {
                // 프리팹 에셋인 경우 동적으로 인스턴스화 수행
                if (string.IsNullOrEmpty(_battleResultView.gameObject.scene.name))
                {
                    var instance = Instantiate(_battleResultView);
                    builder.RegisterComponent(instance);
                }
                else
                {
                    builder.RegisterComponent(_battleResultView);
                }
            }
            else
            {
                builder.RegisterComponentInHierarchy<BattleResultView>();
            }
            builder.RegisterEntryPoint<BattleResultPresenter>(Lifetime.Singleton).AsSelf();
            
            // 캐릭터 UI
            builder.Register<CharacterDetailController>(Lifetime.Scoped);
            builder.RegisterComponentInHierarchy<CharacterDetailPresenter>();
            builder.RegisterComponentInHierarchy<CharacterCombatPanelPresenter>();
        }
    }
}
