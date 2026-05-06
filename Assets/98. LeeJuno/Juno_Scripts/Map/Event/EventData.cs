using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 이벤트 스테이지의 내용을 정의하는 ScriptableObject.
/// 텍스트 설명과 복수의 선택지(EventChoice)로 구성된다.
///
/// 생성 위치: Assets 우클릭 → Create → Bond → Map → EventData
/// </summary>
[CreateAssetMenu(fileName = "EventData", menuName = "Bond/Map/EventData")]
public class EventData : BaseSO
{
    [SerializeField] private List<EventChoice> _choices; // 플레이어가 선택할 수 있는 선택지 목록
    public List<EventChoice> Choices => _choices;
}

/// <summary>
/// 이벤트 내 단일 선택지 데이터.
/// 선택 시 적용될 효과를 EventEffectData 로 정의한다.
/// </summary>
[System.Serializable]
public class EventChoice
{
    [SerializeField] private string _label;               // 선택지 버튼에 표시될 텍스트
    [SerializeField] private string _outcomeDescription;  // 선택 후 표시될 결과 설명
    [SerializeField] private EventEffectData _effect;     // 선택 시 적용될 효과 데이터

    // 하위 호환을 위해 기존 필드는 당분간 유지하되 Obsolete 처리 후 마이그레이션한다.
    [System.Obsolete("EventEffectData 마이그레이션 완료 후 제거")]
    [HideInInspector]
    [SerializeField] private int _hpChange;

    [System.Obsolete("EventEffectData 마이그레이션 완료 후 제거")]
    [HideInInspector]
    [SerializeField] private List<string> _rewardIds;

    /// <summary>선택지 버튼에 표시될 텍스트.</summary>
    public string Label => _label;

    /// <summary>선택 후 표시될 결과 설명.</summary>
    public string OutcomeDescription => _outcomeDescription;

    /// <summary>선택 시 적용될 효과 데이터.</summary>
    public EventEffectData Effect => _effect;
}
