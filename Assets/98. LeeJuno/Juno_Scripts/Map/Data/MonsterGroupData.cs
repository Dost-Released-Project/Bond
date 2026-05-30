using System.Collections.Generic;
using Bond.Expedition;
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

    [Header("등장 던전")]
    public DungeonType DungeonType; // 이 그룹이 등장하는 던전 타입

    /// <summary>
    /// 파서 등 외부에서 프로그래밍 방식으로 데이터를 초기화하는 메서드.
    /// BaseSO.Initialize 에 직접 접근하기 위해 MonsterGroupData 에서 래핑한다.
    /// </summary>
    public void SetData(
        string id,
        string displayName,
        string description,
        List<string> monsterIds,
        int minLayer,
        int maxLayer,
        bool isElite,
        DungeonType dungeonType)
    {
        Initialize(id, displayName, description);
        MonsterIds  = monsterIds;
        MinLayer    = minLayer;
        MaxLayer    = maxLayer;
        IsElite     = isElite;
        DungeonType = dungeonType;
    }
}
