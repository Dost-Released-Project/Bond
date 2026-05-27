using UnityEngine;
using VContainer;
using VContainer.Unity;
using Bond.Expedition;
using Bond.WT.Journal;
using BattleSystem;
using BattleSystem.Interface;
using Bond.UI;
using Cysharp.Threading.Tasks;

namespace Bond.WT.Camping
{
    public class CampingStageEntry : LifetimeScope
    {
        [Header("UI References")]
        [SerializeField] private JournalUIView _journalUIView;
        
        [Header("Formation Settings")]
        [SerializeField] private CharacterSlot[] _slots;

        protected override void Configure(IContainerBuilder builder)
        {
            Debug.Log($"<color=orange>[CampingStageEntry]</color> Configure 시작 - 씬 내 슬롯 개수: {(_slots != null ? _slots.Length : 0)}");

            // 캐릭터 아이템 사용 및 장착 서비스
            builder.Register<CharacterItemService>(Lifetime.Singleton);
            // 아이템 이동 서비스
            builder.Register<InventoryTransferService>(Lifetime.Singleton);
            // 캐릭터 선택 지정
            builder.Register<CharacterSelector>(Lifetime.Singleton).AsImplementedInterfaces().AsSelf();

            // 탐사 인벤토리 UI
            builder.RegisterComponentInHierarchy<ExpeditionInventoryView>();
            // 장신구 슬롯 UI
            builder.RegisterComponentInHierarchy<EquipmentSlotUI>();
            // 캐릭터 슬롯과 캐릭터 셀렉터 연결용
            builder.RegisterComponentInHierarchy<BattleFormationPresenter>();
            // 캠핑 시스템 및 핸들러 등록
            builder.Register<CampingSystem>(Lifetime.Scoped);
            builder.Register<CampingJournalActionHandler>(Lifetime.Scoped).AsImplementedInterfaces().AsSelf();

            // 진영(포메이션) 등록
            builder.Register<FormationManager>(Lifetime.Scoped).As<IFormationManager>();
            if (_slots != null)
            {
                builder.RegisterInstance(_slots);
            }

            // UI 등록
            builder.RegisterJournalUI(_journalUIView);

            builder.RegisterEntryPoint<CampingStageRunner>().AsSelf();

            // 캐릭터 전투 UI
            builder.Register<CharacterDetailController>(Lifetime.Scoped);
            builder.RegisterComponentInHierarchy<CharacterDetailPresenter>();
            builder.RegisterComponentInHierarchy<CharacterCombatPanelPresenter>();
        }
    }

    public class CampingStageRunner : IStartable
    {
        private readonly CampingSystem _campingSystem;
        private readonly CampingJournalActionHandler _actionHandler;
        private readonly IJournalVisualizer _visualizer;
        private readonly IFormationManager _formationManager;
        private readonly ExpeditionPayload _payload;

        public CampingStageRunner(
            CampingSystem campingSystem, 
            CampingJournalActionHandler actionHandler, 
            IJournalVisualizer visualizer,
            IFormationManager formationManager,
            ExpeditionPayload payload)
        {
            _campingSystem = campingSystem;
            _actionHandler = actionHandler;
            _visualizer = visualizer;
            _formationManager = formationManager;
            _payload = payload;

            campingSystem.AddHander(actionHandler);
        }

        public void Start()
        {
            // 캐릭터 모델 슬롯 배치
            if (_payload != null && _payload.Party != null)
            {
                for (int i = 0; i < _payload.Party.Count; i++)
                {
                    if (_payload.Party[i] != null)
                    {
                        _formationManager.SetCharacterToSlot(_payload.Party[i], E_BattleSide.Player, i);
                    }
                }
            }

            // 캠핑 시작
            _campingSystem.StartCamping();
        }
    }
}
