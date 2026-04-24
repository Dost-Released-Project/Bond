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
/// 선택 시 적용될 보상과 HP 변화량을 정의한다.
/// </summary>
[System.Serializable]
public class EventChoice
{
    [SerializeField] private string _label;                 // 선택지 버튼에 표시될 텍스트
    [SerializeField] private string _outcomeDescription;   // 선택 후 표시될 결과 설명
    [SerializeField] private List<string> _rewardIds;      // 선택 시 획득할 보상 ID 목록
    [SerializeField] private int _hpChange;                // HP 증감량 (양수 = 회복, 음수 = 피해)

    public string Label => _label;
    public string OutcomeDescription => _outcomeDescription;
    public List<string> RewardIds => _rewardIds;
    public int HpChange => _hpChange;
}
