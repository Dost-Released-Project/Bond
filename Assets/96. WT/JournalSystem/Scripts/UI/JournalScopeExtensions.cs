using Bond.WT.Journal;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace Bond.WT.Journal
{
    public static class JournalScopeExtensions
    {
        /// <summary>
        /// 특정 씬 스코프(Battle, Town 등)에 일지 시스템의 UI(View)와 연결(Binder)을 등록합니다.
        /// </summary>
        /// <param name="builder">VContainer builder</param>
        /// <param name="journalUIPrefab">생성할 JournalUIView 프리팹</param>
        public static void RegisterJournalUI(this IContainerBuilder builder, JournalUIView journalUIPrefab)
        {
            if (journalUIPrefab != null)
            {
                // UI 프리팹을 런타임에 자동 생성 및 등록 (씬 파괴 시 자동 삭제)
                builder.RegisterComponentInNewPrefab(journalUIPrefab, Lifetime.Scoped)
                    .AsImplementedInterfaces()
                    .AsSelf();
            }
            else
            {
                // 프리팹이 없다면 씬에 이미 배치된 인스턴스를 찾아서 등록
                var existingView = UnityEngine.Object.FindAnyObjectByType<JournalUIView>(FindObjectsInactive.Include);
                if (existingView != null)
                {
                    builder.RegisterComponent(existingView).AsImplementedInterfaces().AsSelf();
                }
                else
                {
                    Debug.LogWarning("[JournalScopeExtensions] JournalUIView 프리팹이 연결되지 않았고, 씬에도 존재하지 않습니다.");
                }
            }
            // Binder는 씬 스코프 단위로 등록되어 씬 파괴 시 함께 Dispose() 됨 -> 구독 안전 해제
            builder.RegisterEntryPoint<JournalBinder>(Lifetime.Singleton).AsSelf();
            }
    }
}