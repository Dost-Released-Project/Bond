using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 전투 스테이지 하나에서 등장할 몬스터 묶음을 정의하는 ScriptableObject.
/// BaseSO.Id 가 MapNode.AssignedMonsterGroupId 와 대응되는 직렬화 키로 사용된다.
/// BaseSO.OnValidate 에 의해 Id 는 반드시 8자리여야 한다.
/// 생성 위치: Assets 우클릭 → Create → Bond → Map → MonsterGroupData
/// </summary>
[CreateAssetMenu(fileName = "MonsterGroupData", menuName = "Bond/Map/MonsterGroupData")]
public class MonsterGroupData : BaseSO
{
    [Header("몬스터 구성")]
    public List<string> MonsterIds; // 이 그룹에 속한 몬스터 ID 목록

    [Header("층 범위 제한")]
    public int MinLayer; // 등장 가능 최소 층 (0 = 제한 없음)
    public int MaxLayer; // 등장 가능 최대 층 (0 = 제한 없음)

    [Header("그룹 등급")]
    public bool IsElite; // true = 엘리트 전용 그룹, false = 노말 전용 그룹
}
