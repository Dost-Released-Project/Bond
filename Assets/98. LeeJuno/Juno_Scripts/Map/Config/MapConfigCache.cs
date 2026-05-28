using System.Collections.Generic;

/// <summary>
/// MapInitializer 가 Addressables 로드 후 Config 데이터를 보관하는 캐시.
/// SO 핸들 해제 전에 필요한 데이터(MapGeneratorConfig, StageConfig 목록,
/// MonsterGroupConfig, EventConfig)를 MapGenerator 와 StageLoader 가 참조할 수 있도록 보관한다.
///
/// SO 해제 타이밍:
///   MapConfigCache 가 SO 참조를 보관하는 동안에는 핸들을 해제하면 안 된다.
///   ReleaseConfigs() 는 챕터 종료 또는 씬 언로드 시점에 호출하고,
///   그 전에 반드시 Clear() 를 호출해 참조를 끊는다.
///
/// VContainer 에 Singleton 으로 등록되며 MapGenerator, StageLoader 가 주입받아 사용한다.
/// </summary>
public class MapConfigCache
{
    /// <summary>맵 절차적 생성 파라미터 Config. IsReady == false 이면 null.</summary>
    public MapGeneratorConfig GeneratorConfig { get; private set; }

    /// <summary>스테이지 타입별 Config 목록. IsReady == false 이면 null.</summary>
    public List<StageConfig> StageConfigs { get; private set; }

    /// <summary>몬스터 그룹 목록 Config. IsReady == false 이면 null.</summary>
    public MonsterGroupConfig MonsterGroupConfig { get; private set; }

    /// <summary>이벤트 목록 Config. IsReady == false 이면 null.</summary>
    public EventConfig EventConfig { get; private set; }

    /// <summary>이벤트 전투 씬 및 몬스터 풀 Config. IsReady == false 이면 null.</summary>
    public EventBattleConfig EventBattleConfig { get; private set; }

    /// <summary>악세사리 DB. IsReady == false 이면 null.</summary>
    public AccessoryDataBaseSO AccessoryDB { get; private set; }

    /// <summary>Set() 이 호출된 이후 true 가 된다.</summary>
    public bool IsReady { get; private set; }

    /// <summary>
    /// MapInitializer 가 Addressables 로드 완료 후 SO 해제 전에 호출한다.
    /// SO 참조를 그대로 보관한다 — 해제 전 호출이 보장되어야 한다.
    /// </summary>
    /// <param name="generatorConfig">맵 절차적 생성 파라미터 Config.</param>
    /// <param name="stageConfigs">스테이지 타입별 Config 목록.</param>
    /// <param name="monsterGroupConfig">몬스터 그룹 목록 Config.</param>
    /// <param name="eventConfig">이벤트 목록 Config.</param>
    /// <param name="eventBattleConfig">이벤트 전투 씬 및 몬스터 풀 Config.</param>
    /// <param name="accessoryDB">악세사리 DB. Addressables 로드 실패 시 null 허용.</param>
    public void Set(
        MapGeneratorConfig generatorConfig,
        List<StageConfig> stageConfigs,
        MonsterGroupConfig monsterGroupConfig,
        EventConfig eventConfig,
        EventBattleConfig eventBattleConfig,
        AccessoryDataBaseSO accessoryDB)
    {
        GeneratorConfig    = generatorConfig;
        StageConfigs       = stageConfigs;
        MonsterGroupConfig = monsterGroupConfig;
        EventConfig        = eventConfig;
        EventBattleConfig  = eventBattleConfig;
        AccessoryDB        = accessoryDB;
        IsReady            = true;
    }

    /// <summary>
    /// 모든 참조를 초기화한다.
    /// ReleaseConfigs() 호출 전 반드시 Clear() 를 먼저 호출해야 한다.
    /// </summary>
    public void Clear()
    {
        GeneratorConfig    = null;
        StageConfigs       = null;
        MonsterGroupConfig = null;
        EventConfig        = null;
        EventBattleConfig  = null;
        AccessoryDB        = null;
        IsReady            = false;
    }
}
