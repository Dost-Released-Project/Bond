using System.Collections.Generic;

/// <summary>
/// Normal 스테이지 씬이 로드되기 직전 StageLoader 가 기록하고,
/// 전투 씬 내부에서 읽어가는 단방향 컨텍스트 채널.
///
/// 선택 이유: Addressables.LoadSceneAsync 는 씬 로드 시 파라미터 직접 전달 불가.
/// VContainer 는 씬 경계를 넘지 않으므로 정적 컨텍스트 클래스가 가장 단순한 해결책.
///
/// 주의: 전투 씬의 진입점에서 데이터를 읽은 뒤 반드시 Clear() 를 호출한다.
/// </summary>
public static class NormalStageContext
{
    /// <summary>현재 배정된 몬스터 그룹 ID.</summary>
    public static string MonsterGroupId { get; private set; } = string.Empty;

    /// <summary>현재 배정된 몬스터 ID 목록.</summary>
    public static List<string> MonsterIds { get; private set; } = new List<string>();

    /// <summary>
    /// StageLoader 가 Normal 스테이지 씬 로드 직전에 호출한다.
    /// 기존 데이터를 덮어쓰고 monsterIds 는 방어적 복사를 수행한다.
    /// </summary>
    /// <param name="groupId">배정된 몬스터 그룹 ID.</param>
    /// <param name="monsterIds">그룹에 속한 몬스터 ID 목록.</param>
    public static void Set(string groupId, List<string> monsterIds)
    {
        MonsterGroupId = groupId;
        // 외부 리스트 변경이 내부 상태에 영향을 미치지 않도록 방어적 복사
        MonsterIds = new List<string>(monsterIds);
    }

    /// <summary>
    /// 전투 씬의 진입점(IBattleEntryPoint 등)에서 데이터를 읽은 뒤 호출한다.
    /// 호출하지 않으면 다음 스테이지 진입 시 이전 데이터가 남아 있을 수 있다.
    /// </summary>
    public static void Clear()
    {
        MonsterGroupId = string.Empty;
        MonsterIds.Clear();
    }
}
