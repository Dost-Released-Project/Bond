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
    private MonsterDataBaseSO m_monsterDb;

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
    ///   대신 MonsterSO의 STR/AGI/INT를 Stat에 직접 세팅한다.
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

        // 스탯 직접 세팅 (Profession 없는 몬스터는 CalcStat() 미호출)
        monster.Stat.STR = so.STR;
        monster.Stat.AGI = so.AGI;
        monster.Stat.INT = so.INT;

        // RoleType.None이면 SetRole() 호출 시 switch 식에서 예외 발생하므로 건너뜀
        if (so.RoleType == RoleType.None == false)
        {
            monster.SetRole(so.RoleType);
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
            $"  STR={monster.Stat.STR}  AGI={monster.Stat.AGI}  INT={monster.Stat.INT}"
        );
        // ────────────────────────────────────────────────────────────────

        return monster;
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
