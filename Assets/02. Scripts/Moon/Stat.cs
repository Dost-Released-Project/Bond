using System.Collections.Generic;
using UnityEngine;

public enum StatType { STR, AGI, INT }

public class Stat : MonoBehaviour
{
    private ClassType classType; // 스탯 보너스를 계산하기 위한 클래스 타입
    
    [Header("Base Stat")]
    public int INT; // int와 구분 짓고자 대문자 사용, 신관 특화 스탯
    public int STR; // 전사 특화 스탯
    public int AGI; // 도적 특화 스탯

    public int insanity; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경

    public Dictionary<Player, int> trust = new Dictionary<Player, int>(); // 딕셔너리 선택 이유: 동료는 언제든 교체될 수 있기에 딕셔너리로 관리 및 저장.

    [Header("STR Stat Bonus(Warrior)")]
    public int max_Hp; // STR x HP_rate (신관 도적 10, 전사 15), ()내용은 임의 비율
    public int current_Hp; // 실제 HP
    public int def; // STR x Def_rate (신관 도적 2, 전사 3)
    public int atk; // STR x Atk_rate (신관 도적 2, 전사 3), 물리 공격력(기본 공격 및 물리 스킬 반영)
    
    [Header("AGI Stat Bonus(Assassin)")]
    public int speed; // AGI x Speed_rate (신관 전사 1, 도적 2), 행동 속도
    public float crt; // AGI x Crt_rate (신관 전사 1, 도적 2), 크리티컬 확률
    public float acc; // AGI x Acc_rate (신관 전사 1, 도적 2), 명중률
    
    [Header("INT Stat Bonus(Cleric)")]
    public float Insanity_Ctrl; // INT x Insanity_Ctrl_rate (전사 도적 1, 신관 2), 스트레스 제어율
    public float Reaction_Ctrl; // INT x Reaction_Ctrl_rate (전사 도적 1, 신관 2), 리액션 통제율
    public int Sp_Atk; // INT x Sp_Atk_rate (전사 도적 2, 신관 3), 특수 스킬 반영

}
