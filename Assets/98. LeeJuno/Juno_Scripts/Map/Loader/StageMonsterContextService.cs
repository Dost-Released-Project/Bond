using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// IStageMonsterContext 구현체.
/// RootScope Singleton으로 등록되어 씬 전환 후에도 동일 인스턴스를 유지한다.
/// 기존 NormalStageContext 정적 클래스의 로직을 인스턴스 기반으로 이전한 구현이다.
/// </summary>
public class StageMonsterContextService : IStageMonsterContext
{
    private string _monsterGroupId = string.Empty;
    private List<string> _monsterIds = new List<string>();

    /// <summary>현재 배정된 몬스터 그룹 ID.</summary>
    public string MonsterGroupId => _monsterGroupId;

    /// <summary>현재 배정된 몬스터 ID 목록 (읽기 전용).</summary>
    public IReadOnlyList<string> MonsterIds
    {
        get
        {
            // TODO: 검증 완료 후 제거
            Debug.Log($"[StageMonsterContextService] MonsterIds 접근 — Count={_monsterIds.Count}");
            return _monsterIds;
        }
    }

    /// <summary>
    /// StageLoader 가 Normal 스테이지 씬 로드 직전에 호출한다.
    /// 기존 데이터를 덮어쓰고 monsterIds 는 방어적 복사를 수행한다.
    /// </summary>
    public void Set(string groupId, List<string> monsterIds)
    {
        _monsterGroupId = groupId;
        // 외부 리스트 변경이 내부 상태에 영향을 미치지 않도록 방어적 복사
        _monsterIds = new List<string>(monsterIds);
        Debug.Log($"[StageMonsterContextService] Set → GroupId='{_monsterGroupId}', MonsterIds={_monsterIds.Count}");
    }

    /// <summary>
    /// 전투 씬의 진입점에서 데이터를 읽은 뒤 호출한다.
    /// 호출하지 않으면 다음 스테이지 진입 시 이전 데이터가 남아 있을 수 있다.
    /// </summary>
    public void Clear()
    {
        _monsterGroupId = string.Empty;
        _monsterIds.Clear();
    }
}
