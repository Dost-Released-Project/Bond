using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 게임 전체의 이벤트 목록을 관리하는 ScriptableObject.
/// MapGenerator 가 Event 노드에 이벤트를 랜덤 배정할 때 참조한다.
/// 생성 위치: Assets 우클릭 → Create → Bond → EventConfig
/// </summary>
[CreateAssetMenu(fileName = "EventConfig", menuName = "Bond/EventConfig")]
public class EventConfig : ScriptableObject
{
    [Header("이벤트 목록")]
    [SerializeField] private List<EventData> _events;
    public List<EventData> Events => _events;
}
