using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// MapConfigLoader 가 Addressables 로드 시 사용할 어드레스 키 묶음.
/// Inspector 에서 키 값을 관리하며 RootScope 에서 RegisterInstance 로 등록한다.
/// 생성 위치: Assets 우클릭 → Create → Bond → MapConfigLoaderSettings
/// </summary>
[CreateAssetMenu(menuName = "Bond/MapConfigLoaderSettings")]
public class MapConfigLoaderSettings : ScriptableObject
{
    [Header("Addressables 키")]
    [SerializeField] private string _mapGeneratorConfigAddress;
    [SerializeField] private string _monsterGroupConfigAddress;
    [SerializeField] private string _bossGroupConfigAddress;
    [SerializeField] private string _eventConfigAddress;
    [SerializeField] private string _eventBattleConfigAddress;

    [Header("StageConfig 키 목록 (StageType 별 1개씩)")]
    [SerializeField] private List<string> _stageConfigAddresses;

    /// <summary>MapGeneratorConfig Addressables 키.</summary>
    public string MapGeneratorConfigAddress => _mapGeneratorConfigAddress;

    /// <summary>MonsterGroupConfig Addressables 키.</summary>
    public string MonsterGroupConfigAddress => _monsterGroupConfigAddress;

    public string BossGroupConfigAddress => _bossGroupConfigAddress;
    /// <summary>EventConfig Addressables 키.</summary>
    public string EventConfigAddress => _eventConfigAddress;

    /// <summary>EventBattleConfig Addressables 키.</summary>
    public string EventBattleConfigAddress => _eventBattleConfigAddress;

    /// <summary>StageConfig Addressables 키 목록.</summary>
    public List<string> StageConfigAddresses => _stageConfigAddresses;
}