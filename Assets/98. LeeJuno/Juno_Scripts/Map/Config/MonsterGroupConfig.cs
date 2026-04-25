using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체의 몬스터 그룹 목록을 관리하는 ScriptableObject.
/// MapGenerator 가 Normal 노드에 그룹을 랜덤 배정할 때 참조한다.
/// 생성 위치: Assets 우클릭 → Create → Bond → MonsterGroupConfig
/// </summary>
[CreateAssetMenu(fileName = "MonsterGroupConfig", menuName = "Bond/MonsterGroupConfig")]
public class MonsterGroupConfig : ScriptableObject
{
    [Header("몬스터 그룹 목록")]
    public List<MonsterGroupData> Groups;
}
