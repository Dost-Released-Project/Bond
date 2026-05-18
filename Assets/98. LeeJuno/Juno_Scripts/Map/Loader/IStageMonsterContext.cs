using System.Collections.Generic;

/// <summary>
/// 맵 씬이 전투 씬으로 전달하는 몬스터 컨텍스트 계약.
/// RootScope Singleton으로 등록되어 맵 씬 StageLoader(쓰기)와
/// 전투 씬 BattleStageEntry(읽기) 양쪽에서 주입받는다.
/// </summary>
public interface IStageMonsterContext
{
    /// <summary>현재 배정된 몬스터 그룹 ID.</summary>
    string MonsterGroupId { get; }

    /// <summary>현재 배정된 몬스터 ID 목록 (읽기 전용).</summary>
    IReadOnlyList<string> MonsterIds { get; }

    /// <summary>
    /// StageLoader 가 Normal 스테이지 씬 로드 직전에 호출한다.
    /// </summary>
    void Set(string groupId, List<string> monsterIds);

    /// <summary>
    /// 전투 씬 진입점에서 데이터를 읽은 뒤 호출한다.
    /// </summary>
    void Clear();
}
