using System.Collections.Generic;

/// <summary>
/// 이벤트 전투 씬 로드 직전 기록되고,
/// 전투 씬 진입점에서 읽어가는 단방향 컨텍스트 채널.
///
/// NormalStageContext 와 동일한 패턴을 따른다.
/// 이벤트 전투는 일반 전투 씬과 별도 주소를 사용할 수도,
/// 동일 씬 주소를 사용할 수도 있다 — EventBattleConfig 에서 결정.
///
/// 주의: 전투 씬의 진입점에서 데이터를 읽은 뒤 반드시 Clear() 를 호출한다.
/// </summary>
public static class EventBattleContext
{
    /// <summary>현재 배정된 몬스터 그룹 ID.</summary>
    public static string MonsterGroupId { get; private set; } = string.Empty;

    /// <summary>현재 배정된 몬스터 ID 목록.</summary>
    public static List<string> MonsterIds { get; private set; } = new List<string>();

    /// <summary>
    /// EventSceneController 에서 Battle 선택지 클릭 시 호출한다.
    /// 기존 데이터를 덮어쓰고 monsterIds 는 방어적 복사를 수행한다.
    /// </summary>
    /// <param name="groupId">배정된 몬스터 그룹 ID.</param>
    /// <param name="monsterIds">그룹에 속한 몬스터 ID 목록.</param>
    public static void Set(string groupId, List<string> monsterIds)
    {
        MonsterGroupId = groupId;
        // 방어적 복사 — 외부 리스트 변경이 내부 상태에 영향을 미치지 않도록
        MonsterIds = new List<string>(monsterIds);
    }

    /// <summary>
    /// StageLoader.TransitionToEventBattleAsync() 에서 NormalStageContext 로 이전 후 호출한다.
    /// 호출하지 않으면 다음 이벤트 전투 진입 시 이전 데이터가 남아 있을 수 있다.
    /// </summary>
    public static void Clear()
    {
        MonsterGroupId = string.Empty;
        MonsterIds.Clear();
    }
}
