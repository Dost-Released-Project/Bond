using VContainer;
using VContainer.Unity;

namespace RootVContainer
{
    // HA(유규하) 파트 전용 RootScope 확장
    public partial class RootScope : LifetimeScope
    {
        /// <summary>
        /// 캐릭터 로스터를 전역 단일 인스턴스로 등록.
        /// 마을→전투→귀환 내내 동일 BaseCharacter 인스턴스를 유지해야 HP/광기 등
        /// 마을 밖에서 일어난 변경이 보존된다(세이브 권위자).
        /// 지연 생성(첫 resolve = Town)이라 생성자의 로드/기본생성이 DBSO 프리로드(Title) 이후에 돈다.
        /// IDisposable 이라 앱 종료/PlayMode 종료 시 미저장분이 플러시된다.
        /// </summary>
        private void ConfigureRoster(IContainerBuilder builder)
        {
            builder.Register<Roster>(Lifetime.Singleton);
        }
    }
}
