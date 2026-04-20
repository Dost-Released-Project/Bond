using UnityEngine;

/// <summary>
/// 스킬 시트 1행에 대응하는 ScriptableObject.
/// 순수 데이터 컨테이너 — 비트마스크 해석 로직은 포함하지 않는다.
///
/// ID 구조: [시트(2)][타입(2)][순번(2)][예약(2)] = 8자리
/// 비트마스크:
///   - UseableSlots / AllyTargetMask : 아군 기준 [3][2][1][0]
///   - EnemyTargetMask               : 적 기준   [0][1][2][3]
/// </summary>
using UnityEngine;

[CreateAssetMenu(menuName = "Bond/SkillSystem/SkillData", fileName = "SK_00000000")]
public class SkillData : BaseSO
{
    [Header("타입")]
    [SerializeField] private SkillType   _type;
    [SerializeField] private SkillTarget _target;

    [Header("수치")]
    [Tooltip("기본 효과 수치 (데미지 / 힐 / 버프량)")]
    [SerializeField] private float _value;

    [Tooltip("쿨타임 (0 = 없음, N = N라운드 후 재사용)")]
    [SerializeField] private int _coolTime;

    [Tooltip("지속 시간 (0 = 즉발, N = N턴 지속)")]
    [SerializeField] private int _duration;

    [Header("사용 조건 (비트마스크 원시값)")]
    [Tooltip("사용 가능 클래스 비트마스크. 0 = 모든 클래스 허용.")]
    [SerializeField] private int _useableClasses;

    [Tooltip("사용 가능 칸 비트마스크 (4비트). 아군 기준: [3][2][1][0]")]
    [SerializeField] private int _useableSlots;

    [Header("범위 (비트마스크 원시값)")]
    [Tooltip("적 진영 타겟 비트마스크 (4비트). 적 기준: [0][1][2][3]")]
    [SerializeField] private int _enemyTargetMask;

    [Tooltip("아군 진영 타겟 비트마스크 (4비트). 아군 기준: [3][2][1][0]")]
    [SerializeField] private int _allyTargetMask;

    [Header("비주얼")]
    [Tooltip("Addressables 아이콘 주소")]
    [SerializeField] private string _iconAddress;

    // ── 프로퍼티 ────────────────────────────────────
    // 기반 필드(Id, DisplayName, Description)는 BaseSO 프로퍼티 그대로 사용.
    // SkillId / SkillName 별칭 제거 — 호출부를 Id / DisplayName 으로 통일.
    public SkillType   Type           => _type;
    public SkillTarget Target         => _target;
    public float       Value          => _value;
    public int         CoolTime       => _coolTime;
    public int         Duration       => _duration;
    public int         UseableClasses => _useableClasses;
    public int         UseableSlots   => _useableSlots;
    public int         EnemyTargetMask=> _enemyTargetMask;
    public int         AllyTargetMask => _allyTargetMask;
    public string      IconAddress    => _iconAddress;

    /// <summary>
    /// 파서가 TSV 1행을 읽어 채운 SkillRawData를 받아 필드를 초기화한다.
    /// 기반 필드는 base.Initialize()를 통해서만 쓴다.
    /// </summary>
    public void SetData(in SkillRawData raw)    // ← struct는 in으로 복사 비용 절감
    {
        Initialize(raw.SkillId, raw.SkillName, raw.Description);  // BaseSO 기반 필드

        _type            = raw.Type;
        _target          = raw.Target;
        _value           = raw.Value;
        _coolTime        = raw.CoolTime;
        _duration        = raw.Duration;
        _useableClasses  = raw.UseableClasses;
        _useableSlots    = raw.UseableSlots;
        _enemyTargetMask = raw.EnemyTargetMask;
        _allyTargetMask  = raw.AllyTargetMask;
        _iconAddress     = raw.IconAddress;
    }
}

/// <summary>
/// SkillData.SetData() 에 넘길 파라미터 묶음.
/// 파서가 TSV 1행을 읽어 이 구조체를 채운 뒤 SetData()에 전달한다.
/// </summary>
public struct SkillRawData
{
    public string     SkillId;
    public string     SkillName;
    public string     Description;
    public SkillType  Type;
    public SkillTarget Target;
    public float      Value;
    public int        CoolTime;
    public int        Duration;
    public int        UseableClasses;
    public int        UseableSlots;
    public int        EnemyTargetMask;
    public int        AllyTargetMask;
    public string     IconAddress;
}
