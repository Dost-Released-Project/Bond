using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;

/// <summary>
/// MonsterSO ID 목록을 BaseCharacter 배열로 변환하는 팩토리.
/// StageCoach(플레이어 캐릭터 빌더)와 대칭 구조로 설계되었다.
/// MonsterDataBaseSO를 Addressables에서 지연 로드한 뒤 ID별로 SO를 조회하고
/// BaseCharacter를 조립해 반환한다.
/// </summary>
public class MonsterFactory
{
    // ── 파생 스탯 배율 상수 ───────────────────────────────────────────────────
    // Profession.CalculateStat()의 ClassSO 배율에 대응한다.
    // MonsterSO에 직업 데이터가 없으므로 팩토리 내부에 고정값으로 정의한다.
    private const float HpMultiplier           = 5f;  // STR 계열: max_Hp
    private const float AtkMultiplier          = 2f;  // STR 계열: atk
    private const float DefMultiplier          = 1f;  // AGI 계열: def
    private const float SpeedMultiplier        = 3f;  // AGI 계열: speed
    private const float CriMultiplier          = 1f;  // AGI 계열: crt
    private const float AccMultiplier          = 2f;  // AGI 계열: acc
    private const float InsanityCtrlMultiplier = 1f;  // INT 계열: Insanity_Ctrl
    private const float ReactionCtrlMultiplier = 1f;  // INT 계열: Reaction_Ctrl
    private const float SpAtkMultiplier        = 2f;  // INT 계열: Sp_Atk
    // ─────────────────────────────────────────────────────────────────────────

    private MonsterDataBaseSO m_monsterDb;
    private readonly ISkillManager m_skillManager;

    public MonsterFactory(ISkillManager skillManager)
    {
        m_skillManager = skillManager;
    }

    /// <summary>
    /// IStageMonsterContext.MonsterIds를 받아 BaseCharacter 배열을 생성한다.
    /// DB 미로드 시 지연 초기화 후 진행한다.
    /// </summary>
    public BaseCharacter[] Build(IReadOnlyList<string> monsterIds)
    {
        EnsureDb();

        List<BaseCharacter> result = new List<BaseCharacter>();

        for (int i = 0; i < monsterIds.Count; i++)
        {
            MonsterSO so = m_monsterDb.GetSO<MonsterSO>(monsterIds[i]);
            if (so == null)
            {
                Debug.LogWarning($"[MonsterFactory] MonsterSO를 찾을 수 없습니다. ID={monsterIds[i]}");
                continue;
            }

            BaseCharacter monster = CreateFromSO(so);
            result.Add(monster);
        }

        return result.ToArray();
    }

    /// <summary>
    /// MonsterSO 데이터를 기반으로 BaseCharacter를 조립한다.
    /// BaseCharacter.Sample을 통해 인스턴스를 얻은 뒤 필드를 직접 채운다.
    /// (BaseCharacterData 생성자가 private이므로 Sample 경유 패턴을 따른다)
    ///
    /// CalcStat()을 호출하지 않는 이유:
    ///   CalcStat()은 내부에서 Data.Profession.CalculateStat()을 호출한다.
    ///   MonsterSO에는 ClassSO에 해당하는 직업 데이터가 없으므로 Data.Profession이 null이 되어
    ///   NullReferenceException이 발생한다.
    ///   대신 CalculateDerivedStats()에서 팩토리 내부 배율 상수로 파생 스탯을 직접 계산한다.
    /// </summary>
    private BaseCharacter CreateFromSO(MonsterSO so)
    {
        BaseCharacter monster = BaseCharacter.Sample;

        // Data 필드 설정
        monster.Id           = so.Id;
        monster.Name         = so.DisplayName;
        monster.Level        = so.Level;
        monster.RoleType     = so.RoleType;
        monster.ImageAddress = so.ImageAddress;

        // 기본 능력치 세팅
        monster.Stat.STR = so.STR;
        monster.Stat.AGI = so.AGI;
        monster.Stat.INT = so.INT;

        // 파생 스탯 계산 (Profession 없는 몬스터용 직접 계산)
        CalculateDerivedStats(monster);

        // RoleType.None이면 SetRole() 호출 시 switch 식에서 예외 발생하므로 건너뜀
        if (so.RoleType == RoleType.None == false)
        {
            monster.SetRole(so.RoleType);
        }

        // 스킬 장착 로직
        if (so.SkillIds != null && so.SkillIds.Count > 0)
        {
            int maxSkills = Mathf.Min(so.SkillIds.Count, 4);
            for (int i = 0; i < maxSkills; i++)
            {
                if (string.IsNullOrEmpty(so.SkillIds[i]) == false)
                {
                    SkillData skillData = m_skillManager.GetSkill(so.SkillIds[i]);
                    if (skillData != null)
                    {
                        MonsterSkill mSkill = new MonsterSkill();
                        // 1. SkillBase의 기존 Init 호출을 통해 _skillData 및 Cooldown 셋팅
                        mSkill.Init(skillData.Id, m_skillManager);
                        // 2. 몬스터 스탯을 기반으로 데미지 보정 적용
                        mSkill.ApplyStat(monster.Stat);
                        
                        monster.Skills[i] = mSkill;
                        
                        Debug.Log($"[MonsterFactory] {monster.Name}에 '{skillData.DisplayName}'(ID:{skillData.Id}) 장착. 사용가능슬롯(Mask): {skillData.UseableSlots}");
                    }
                    else
                    {
                        Debug.LogWarning($"[MonsterFactory] SkillData를 찾을 수 없습니다. ID={so.SkillIds[i]}");
                    }
                }
            }
        }

        // ── 테스트 로그 ─────────────────────────────────────────────────
        // BaseCharacter.ToString()은 Profession.Name을 참조하므로
        // 몬스터(Profession == null)에 사용하면 NullReferenceException이 발생한다.
        // 개별 필드를 직접 참조하는 포맷 문자열로 출력한다.
        Debug.Log(
            $"[MonsterFactory] 몬스터 생성 완료\n" +
            $"  ID       : {monster.Id}\n" +
            $"  Name     : {monster.Name}\n" +
            $"  Level    : {monster.Level}\n" +
            $"  RoleType : {monster.RoleType}\n" +
            $"  STR={monster.Stat.STR}  AGI={monster.Stat.AGI}  INT={monster.Stat.INT}\n" +
            $"  HP={monster.Stat.max_Hp}  ATK={monster.Stat.atk}  DEF={monster.Stat.def}\n" +
            $"  SPD={monster.Stat.speed}  CRI={monster.Stat.crt}  ACC={monster.Stat.acc}"
        );
        // ────────────────────────────────────────────────────────────────

        return monster;
    }

    /// <summary>
    /// Profession 없는 몬스터의 파생 스탯을 계산한다.
    /// ClassSO 배율 대신 MonsterFactory 내 배율 상수를 사용하며,
    /// 계산 방식은 Profession.CalculateStat()과 동일하다.
    /// 계산 완료 후 current_Hp를 max_Hp로 초기화한다.
    /// </summary>
    private static void CalculateDerivedStats(BaseCharacter monster)
    {
        StatController controller = monster.StatController;
        Stat stat = monster.Stat;

        // 모디파이어 적용 (몬스터는 특수 모디파이어 없으므로 입력값 그대로 반환됨)
        float finalSTR = controller.ApplyModifiers(StatType.STR, stat.STR);
        float finalAGI = controller.ApplyModifiers(StatType.AGI, stat.AGI);
        float finalINT = controller.ApplyModifiers(StatType.INT, stat.INT);

        // STR 계열
        stat.max_Hp = Mathf.RoundToInt(controller.ApplyModifiers(StatType.MaxHP, finalSTR * HpMultiplier));
        stat.atk    = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Atk,   finalSTR * AtkMultiplier));

        // AGI 계열
        stat.def   = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Def,   finalAGI * DefMultiplier));
        stat.speed = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Speed, finalAGI * SpeedMultiplier));
        stat.crt   = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Cri,   finalAGI * CriMultiplier));
        stat.acc   = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Acc,   finalAGI * AccMultiplier));

        // INT 계열
        stat.Insanity_Ctrl = Mathf.RoundToInt(controller.ApplyModifiers(StatType.InsanityCtrl, finalINT * InsanityCtrlMultiplier));
        stat.Reaction_Ctrl = Mathf.RoundToInt(controller.ApplyModifiers(StatType.ReactionCtrl, finalINT * ReactionCtrlMultiplier));
        stat.Sp_Atk        = Mathf.RoundToInt(controller.ApplyModifiers(StatType.SpAtk,        finalINT * SpAtkMultiplier));

        // 현재 체력을 최대 체력으로 초기화
        stat.current_Hp = stat.max_Hp;
    }

    /// <summary>
    /// MonsterDataBaseSO 지연 초기화.
    /// 최초 Build() 호출 시점에만 Addressables 로드가 발생한다.
    /// </summary>
    private void EnsureDb()
    {
        if (m_monsterDb == null == false)
            return;

        m_monsterDb = Addressables
            .LoadAssetAsync<MonsterDataBaseSO>("MonsterDataBase")
            .WaitForCompletion();

        if (m_monsterDb == null)
            Debug.LogError("[MonsterFactory] MonsterDataBase Addressable 로드 실패. Addressables에 'MonsterDataBase' 키가 등록되어 있는지 확인한다.");
    }
}
