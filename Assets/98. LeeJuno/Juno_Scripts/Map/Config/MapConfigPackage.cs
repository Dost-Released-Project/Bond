using System.Collections.Generic;

/// <summary>
/// MapConfigLoader 가 로드 완료한 Config SO 를 MapGenerator 로 전달하는 데이터 전송 객체.
/// ScriptableObject 를 직접 보관하므로 ReleaseConfigs() 호출 후에는 참조를 사용하지 않는다.
/// 생성 후 외부에서 필드를 변경할 수 없도록 생성자 초기화 패턴을 사용한다.
/// </summary>
public class MapConfigPackage
{
    /// <summary>맵 절차적 생성 파라미터 Config.</summary>
    public MapGeneratorConfig GeneratorConfig { get; private set; }

    /// <summary>스테이지 타입별 Config 목록.</summary>
    public List<StageConfig> StageConfigs { get; private set; }

    /// <summary>몬스터 그룹 목록 Config.</summary>
    public MonsterGroupConfig MonsterGroupConfig { get; private set; }

    /// <summary>이벤트 목록 Config.</summary>
    public EventConfig EventConfig { get; private set; }

    /// <summary>이벤트 전투 씬 및 몬스터 풀 Config.</summary>
    public EventBattleConfig EventBattleConfig { get; private set; }

    /// <summary>
    /// 모든 Config 를 한 번에 초기화하는 생성자.
    /// MapConfigLoader 에서만 생성한다.
    /// </summary>
    public MapConfigPackage(
        MapGeneratorConfig generatorConfig,
        List<StageConfig> stageConfigs,
        MonsterGroupConfig monsterGroupConfig,
        EventConfig eventConfig,
        EventBattleConfig eventBattleConfig)
    {
        GeneratorConfig    = generatorConfig;
        StageConfigs       = stageConfigs;
        MonsterGroupConfig = monsterGroupConfig;
        EventConfig        = eventConfig;
        EventBattleConfig  = eventBattleConfig;
    }
}
