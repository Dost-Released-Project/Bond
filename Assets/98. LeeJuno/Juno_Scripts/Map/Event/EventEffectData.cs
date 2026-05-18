using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이벤트 선택지 하나에 결합되는 효과 파라미터.
/// EventChoice._effect 필드로 직렬화된다.
/// Inspector 에서 EffectType 을 먼저 선택하면 관련 필드만 표시되도록
/// CustomEditor 또는 PropertyDrawer 작성 권장.
/// </summary>
[System.Serializable]
public class EventEffectData
{
    [SerializeField] private EffectType _effectType;

    /// <summary>효과 종류.</summary>
    public EffectType EffectType => _effectType;

    // ── EffectType = HpChange / StatusEffect 공통 ──────────────
    [SerializeField] private TargetType _targetType;

    /// <summary>효과 적용 대상 선택 방식.</summary>
    public TargetType TargetType => _targetType;

    // ── EffectType = HpChange ──────────────────────────────────
    [SerializeField] private int _hpChangeAmount; // 양수 = 회복, 음수 = 피해

    /// <summary>HP 증감량. 양수는 회복, 음수는 피해.</summary>
    public int HpChangeAmount => _hpChangeAmount;

    // ── EffectType = StatusEffect ──────────────────────────────
    [SerializeField] private string _statusEffectId; // 미구현 — TODO: StatusEffectData SO ID

    /// <summary>상태이상 ID. 상태이상 시스템 구현 후 연동 예정.</summary>
    public string StatusEffectId => _statusEffectId;

    // ── EffectType = ItemReward ────────────────────────────────
    [SerializeField] private ItemRewardType _itemRewardType;

    /// <summary>아이템 획득 방식.</summary>
    public ItemRewardType ItemRewardType => _itemRewardType;

    [SerializeField] private string _guaranteedItemId; // Guaranteed 전용

    /// <summary>무조건 획득할 아이템 ID. ItemRewardType == Guaranteed 일 때 사용.</summary>
    public string GuaranteedItemId => _guaranteedItemId;

    [Range(0f, 1f)]
    [SerializeField] private float _itemProbability; // Probability 전용 (0~1)

    /// <summary>아이템 획득 확률. ItemRewardType == Probability 일 때 사용 (0~1).</summary>
    public float ItemProbability => _itemProbability;

    [SerializeField] private string _probabilityItemId; // Probability 전용

    /// <summary>확률 획득 대상 아이템 ID. ItemRewardType == Probability 일 때 사용.</summary>
    public string ProbabilityItemId => _probabilityItemId;

    [SerializeField] private List<string> _itemPool = new List<string>(); // RandomFromPool 전용

    /// <summary>랜덤 획득 아이템 풀. ItemRewardType == RandomFromPool 일 때 사용.</summary>
    public List<string> ItemPool => _itemPool;

    // ── EffectType = Battle ────────────────────────────────────
    // 몬스터 풀은 EventBattleConfig SO 에서 관리한다.
    // 여기서는 "이 선택지가 전투를 트리거한다"는 플래그 역할만 한다.
    // 별도 필드 불필요 — EffectType == Battle 자체가 플래그.

    /// <summary>
    /// 파서에서 호출하는 초기화 메서드.
    /// 모든 필드가 private 이므로 EventEffectData 내부에서만 접근 가능하다.
    /// </summary>
    public void SetData(
        EffectType     effectType,
        TargetType     targetType,
        int            hpChangeAmount,
        string         statusEffectId,
        ItemRewardType itemRewardType,
        string         guaranteedItemId,
        float          itemProbability,
        string         probabilityItemId,
        List<string>   itemPool)
    {
        _effectType        = effectType;
        _targetType        = targetType;
        _hpChangeAmount    = hpChangeAmount;
        _statusEffectId    = statusEffectId;
        _itemRewardType    = itemRewardType;
        _guaranteedItemId  = guaranteedItemId;
        _itemProbability   = itemProbability;
        _probabilityItemId = probabilityItemId;
        _itemPool          = itemPool ?? new List<string>();
    }
}
