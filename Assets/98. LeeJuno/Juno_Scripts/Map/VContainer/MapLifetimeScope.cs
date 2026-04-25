using System.Collections.Generic;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 맵 시스템의 VContainer 의존성 등록 스코프.
/// 이 씬에 올라가 있으면 맵 시스템 전체가 DI 컨테이너로 관리된다.
///
/// 등록 목록:
///   - MapGeneratorConfig  : 맵 생성 파라미터 (Instance)
///   - List(StageConfig)   : 스테이지 타입별 설정 (Instance)
///   - MonsterGroupConfig  : 몬스터 그룹 목록 (Instance)
///   - IMapGenerator       → MapGenerator (Singleton)
///   - IMapRepository      → MapRepository (Singleton)
///   - IMapNavigator       → MapNavigator  (Singleton)
///   - IStageLoader        → StageLoader   (Singleton)
///   - MapUIController     : MonoBehaviour 컴포넌트 등록
///
/// Inspector 연결 필요:
///   _generatorConfig     — MapGeneratorConfig ScriptableObject
///   _stageConfigs        — StageConfig 목록 (Normal, Elite, Boss, Camping, Event, Shop 각 1개)
///   _monsterGroupConfig  — MonsterGroupConfig ScriptableObject
///   _mapUIController     — 씬에 배치된 MapUIController MonoBehaviour
/// </summary>
public class MapLifetimeScope : LifetimeScope
{
    [SerializeField] private MapGeneratorConfig _generatorConfig;
    [SerializeField] private List<StageConfig> _stageConfigs;
    [SerializeField] private MonsterGroupConfig _monsterGroupConfig; // Inspector 연결 필요
    [SerializeField] private MapUIController _mapUIController;

    protected override void Configure(IContainerBuilder builder)
    {
        // ScriptableObject 인스턴스를 컨테이너에 직접 등록
        builder.RegisterInstance(_generatorConfig);
        builder.RegisterInstance(_stageConfigs);
        builder.RegisterInstance(_monsterGroupConfig);

        // 맵 시스템 핵심 서비스 등록 (인터페이스 → 구현체 바인딩)
        builder.Register<IMapGenerator, MapGenerator>(Lifetime.Singleton);
        builder.Register<IMapRepository, MapRepository>(Lifetime.Singleton);
        builder.Register<IMapNavigator, MapNavigator>(Lifetime.Singleton);
        builder.Register<IStageLoader, StageLoader>(Lifetime.Singleton);

        // 씬에 배치된 MonoBehaviour를 DI 대상으로 등록
        builder.RegisterComponent(_mapUIController);

        builder.RegisterEntryPoint<MapTestStarter>();
    }
}