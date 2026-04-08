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
[CreateAssetMenu(menuName = "Bond/SkillSystem/SkillData", fileName = "SK_00000000")]
public class SkillData : ScriptableObject
{
    // ── 식별자 ────────────────────────────────────
    [Header("식별자")]
    [Tooltip("스킬 시트 ID (8자리): 예) 01010000")]
    [SerializeField] private int _skillId;

    [Tooltip("표시용 스킬 이름")]
    [SerializeField] private string _skillName;

    [Tooltip("스킬 설명 텍스트")]
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    // ── 타입 ──────────────────────────────────────
    [Header("타입")]
    [SerializeField] private SkillType _type;
    [SerializeField] private SkillTarget _target;

    // ── 수치 ──────────────────────────────────────
    [Header("수치")]
    [Tooltip("기본 효과 수치 (데미지 / 힐 / 버프량)")]
    [SerializeField] private float _value;

    [Tooltip("쿨타임 (0 = 없음, N = N라운드 후 재사용)")]
    [SerializeField] private int _coolTime;

    [Tooltip("지속 시간 (0 = 즉발, N = N턴 지속)")]
    [SerializeField] private int _duration;

    // ── 사용 조건 (비트마스크 원시값) ──────────────
    [Header("사용 조건 (비트마스크 원시값)")]
    [Tooltip("사용 가능 클래스 비트마스크. 0 = 모든 클래스 허용.")]
    [SerializeField] private int _useableClasses;

    [Tooltip("사용 가능 칸 비트마스크 (4비트). 아군 기준: [3][2][1][0]")]
    [SerializeField] private int _useableSlots;

    // ── 범위 (비트마스크 원시값) ───────────────────
    [Header("범위 (비트마스크 원시값)")]
    [Tooltip("적 진영 타겟 비트마스크 (4비트). 적 기준: [0][1][2][3]")]
    [SerializeField] private int _enemyTargetMask;

    [Tooltip("아군 진영 타겟 비트마스크 (4비트). 아군 기준: [3][2][1][0]")]
    [SerializeField] private int _allyTargetMask;

    // ── 비주얼 ────────────────────────────────────
    [Header("비주얼")]
    [Tooltip("Addressables 아이콘 주소 (시트 아이콘 ID)")]
    [SerializeField] private string _iconAddress;

    // ── 프로퍼티 (읽기 전용) ───────────────────────
    public int SkillId            => _skillId;
    public string SkillName       => _skillName;
    public string Description     => _description;
    public SkillType Type         => _type;
    public SkillTarget Target     => _target;
    public float Value            => _value;
    public int CoolTime           => _coolTime;
    public int Duration           => _duration;
    public int UseableClasses     => _useableClasses;
    public int UseableSlots       => _useableSlots;
    public int EnemyTargetMask    => _enemyTargetMask;
    public int AllyTargetMask     => _allyTargetMask;
    public string IconAddress     => _iconAddress;

    /// <summary>
    /// 파서 등 외부 코드에서 프로그래밍 방식으로 값을 설정할 때 사용한다.
    /// ScriptableObject.CreateInstance&lt;SkillData&gt;() 로 생성 후 호출.
    /// Inspector 직렬화 필드를 그대로 사용하므로 기존 에셋과 호환된다.
    /// </summary>
    public void SetData(SkillRawData raw)
    {
        _skillId         = raw.SkillId;
        _skillName       = raw.SkillName;
        _description     = raw.Description;
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
    public int        SkillId;
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
