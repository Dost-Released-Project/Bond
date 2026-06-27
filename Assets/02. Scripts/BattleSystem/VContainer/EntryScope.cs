using BattleSystem.Interface;
using Bond.Tutorial;
using UnityEngine;
using UnityEngine.Serialization;
using VContainer;
using VContainer.Unity;

namespace BattleSystem.VContainer
{
    public class EntryScope : LifetimeScope
    {
        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterEntryPoint<BattleStageEntry>(Lifetime.Singleton).As<IBattleStageEntry>();
            builder.Register<MonsterFactory>(Lifetime.Singleton);
            
            // 순수 C# 코어 컨트롤러 등록 (프로젝트 스코프에 이미 Singleton으로 등록되어 있다면 제외 가능)
            builder.Register<BattleTutorialSystemController>(Lifetime.Singleton);
            // 어드레서블 로드 및 컨트롤러 초기화 바인딩을 담당할 엔트리 포인트 등록
            builder.Register<BattleTutorialEntryPoint>(Lifetime.Singleton).AsImplementedInterfaces();
            // 씬에 배치된 UI Toolkit 마스킹 뷰 등록
            builder.RegisterComponentInHierarchy<TutorialBattleView>();
        }
    }
}
