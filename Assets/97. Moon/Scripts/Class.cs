using UnityEngine;

public enum ClassType { Warrior, Assassin, Cleric }

public class Class : MonoBehaviour
{
    ClassType classType { get; set; }
    // 전사 도적 신관 3개의 클래스 존재.(enum으로 정의)
    // 전사는 STR 스탯에 추가 보정을 받음
    // 도적은 AGI 스탯에 추가 보정을 받음
    // 신관은 INT 스탯에 추가 보정을 받음
    
    // 즉, 이 클래스는 캐릭터의 클래스를 파악, 스탯 보정을 주는 역할.
}