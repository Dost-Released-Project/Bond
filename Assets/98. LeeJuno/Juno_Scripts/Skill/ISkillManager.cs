using System.Collections.Generic;

/// <summary>
/// 스킬 데이터 접근 및 쿨타임 관리를 위한 인터페이스.
/// VContainer 싱글턴으로 등록되어 SkillBase, TurnManager 등에서 주입받아 사용한다.
/// 비트마스크 해석(슬롯 매핑 등)은 다른 팀원의 매니저가 담당하므로
/// 여기서는 ID/타입 기반 조회와 쿨타임 관리만 제공한다.
/// </summary>
public interface ISkillManager
{
    // ── 데이터 조회 ───────────────────────────────

    /// <summary>ID로 스킬 데이터 단건 조회. 없으면 null 반환.</summary>
    SkillData GetSkill(string skillId);

    /// <summary>특정 타입의 모든 스킬 목록 반환.</summary>
    IReadOnlyList<SkillData> GetSkillsByType(SkillType type);

    /// <summary>전체 스킬 목록 반환.</summary>
    IReadOnlyList<SkillData> GetAllSkills();

    // ── 쿨타임 관리 ───────────────────────────────

    /// <summary>스킬 사용 직후 호출. 해당 스킬의 쿨타임을 등록한다.</summary>
    void RegisterCoolTime(BaseCharacter character, string skillId, int coolTime);

    /// <summary>라운드 종료 시 호출. 유닛의 모든 스킬 쿨타임을 1 감소시킨다.</summary>
    void TickCoolTimes(BaseCharacter character);

    /// <summary>유닛의 특정 스킬 남은 쿨타임 반환. 0이면 사용 가능.</summary>
    int GetRemainingCoolTime(BaseCharacter character, string skillId);
}
