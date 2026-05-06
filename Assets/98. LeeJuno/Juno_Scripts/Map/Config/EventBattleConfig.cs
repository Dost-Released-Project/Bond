using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이벤트 전투 씬에서 사용할 몬스터 그룹 풀을 정의하는 ScriptableObject.
/// EventChoice.Effect.EffectType == Battle 인 선택지가 트리거되면
/// 이 풀에서 랜덤으로 그룹 ID 하나를 뽑아 NormalStageContext 에 기록한다.
///
/// 생성 위치: Assets 우클릭 → Create → Bond → Map → EventBattleConfig
/// </summary>
[CreateAssetMenu(fileName = "EventBattleConfig", menuName = "Bond/Map/EventBattleConfig")]
public class EventBattleConfig : ScriptableObject
{
    [Header("이벤트 전투 씬 주소 (Addressables)")]
    [SerializeField] private string _eventBattleSceneAddress;

    /// <summary>이벤트 전투 씬 Addressables 키.</summary>
    public string EventBattleSceneAddress => _eventBattleSceneAddress;

    [Header("이벤트 전투 몬스터 그룹 풀")]
    [SerializeField] private List<MonsterGroupData> _monsterGroupPool;

    /// <summary>이벤트 전투에 등장 가능한 몬스터 그룹 풀 목록.</summary>
    public IReadOnlyList<MonsterGroupData> MonsterGroupPool => _monsterGroupPool;
}
