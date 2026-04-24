using UnityEngine;

/// <summary>
/// 모든 데이터 SO의 공통 기반.
/// 외부 쓰기는 Initialize() 한 곳으로만 허용한다.
/// </summary>
public abstract class BaseSO : ScriptableObject
{
    [Header("식별자")]
    [Tooltip("시트 ID (8자리): 예) 01010000")]
    [SerializeField] private string _id;

    [Tooltip("표시용 이름 — Object.name과 구분")]
    [SerializeField] private string _displayName;

    [Tooltip("설명 텍스트")]
    [TextArea(2, 4)]
    [SerializeField] private string _description;

    // ── 읽기 전용 프로퍼티 ──────────────────────────
    public string Id          => _id;
    public string DisplayName => _displayName;
    public string Description => _description;

    /// <summary>
    /// 파서 등 외부에서 프로그래밍 방식으로 기반 필드를 초기화.
    /// SetData()에서 base.Initialize()로 호출한다.
    /// </summary>
    protected void Initialize(string id, string displayName, string description)
    {
        _id          = id;
        _displayName = displayName;
        _description = description;
    }

#if UNITY_EDITOR
    protected virtual void OnValidate()
    {
        if (!string.IsNullOrEmpty(_id) && _id.Length != 8)
            Debug.LogWarning($"[BaseSO] ID는 8자리여야 합니다: '{_id}' ({name})", this);
    }
#endif
}