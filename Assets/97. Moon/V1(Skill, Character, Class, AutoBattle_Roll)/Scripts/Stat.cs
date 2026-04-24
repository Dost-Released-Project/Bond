using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using Random = UnityEngine.Random;

public enum StatType { STR, AGI, INT }

public class Stat : MonoBehaviour
{
    private ClassType classType; // 스탯 보너스를 계산하기 위한 클래스 타입
    public ClassType ClassType => classType;
    
    [Header("Base Stat")]
    public int INT { get; private set; } // int와 구분 짓고자 대문자 사용, 신관 특화 스탯
    public int STR { get; private set; } // 전사 특화 스탯
    public int AGI { get; private set; } // 도적 특화 스탯

    public int insanity; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경

    public Dictionary<BaseCharacter, int> trust = new Dictionary<BaseCharacter, int>(); // 딕셔너리 선택 이유: 동료는 언제든 교체될 수 있기에 딕셔너리로 관리 및 저장.

    [Header("STR Stat Bonus(Warrior)")]
    public int max_Hp { get; private set; } // STR x HP_rate (신관 도적 10, 전사 15), ()내용은 임의 비율
    public int current_Hp { get; private set; } // 실제 HP
    public int def { get; private set; } // STR x Def_rate (신관 도적 2, 전사 3)
    public int atk { get; private set; } // STR x Atk_rate (신관 도적 2, 전사 3), 물리 스킬 반영
    
    [Header("AGI Stat Bonus(Assassin)")]
    public int speed { get; private set; } // AGI x Speed_rate (신관 전사 1, 도적 2), 행동 속도
    public float crt { get; private set; } // AGI x Crt_rate (신관 전사 1, 도적 2), 크리티컬 확률
    public float acc { get; private set; } // AGI x Acc_rate (신관 전사 1, 도적 2), 명중률
    
    [Header("INT Stat Bonus(Cleric)")]
    public float Insanity_Ctrl { get; private set; } // INT x Insanity_Ctrl_rate (전사 도적 1, 신관 2), 스트레스 제어율
    public float Reaction_Ctrl { get; private set; } // INT x Reaction_Ctrl_rate (전사 도적 1, 신관 2), 리액션 통제율
    public int Sp_Atk { get; private set; } // INT x Sp_Atk_rate (전사 도적 2, 신관 3), 주문 스킬 반영

    private void Start()
    {
        // 기본 스탯 랜덤
        STR = Random.Range(1,6);
        AGI = Random.Range(1,6);
        INT = Random.Range(1,6);

        // 클래스 타입 랜덤
        classType = (ClassType)Random.Range(0, 3);
    }
    
    public void StatCalculate(string str) // 테스트용으로 스트링 매개 변수를 추가한 스탯 계산 로직
    {
        if (classType == ClassType.Warrior) // 전사 특화 스탯
        {
            max_Hp = STR * 15;
            def = STR * 3;
            atk = STR * 3;
        }
        else
        {
            max_Hp = STR * 10;
            def = STR * 2;
            atk = STR * 2;
        }

        if (classType == ClassType.Assassin) // 도적 특화 스탯
        {
            speed = AGI * 2;
            crt = AGI * 2;
            acc = AGI * 2;
        }
        else
        {
            speed = AGI * 1;
            crt = AGI * 1;
            acc = AGI * 1;
        }

        if (classType == ClassType.Cleric) // 신관 특화 스탯
        {
            Insanity_Ctrl = INT * 2;
            Reaction_Ctrl = INT * 2;
            Sp_Atk = INT * 3;
        }
        else
        {
            Insanity_Ctrl = INT * 1;
            Reaction_Ctrl = INT * 1;
            Sp_Atk = INT * 2;
        }

        current_Hp = max_Hp;
        Debug.Log($"{str}의 현재 스탯은 다음과 같습니다.\nSTR: {STR} AGI: {AGI} INT: {INT}\nHP: {max_Hp} DEF: {def} ATK: {atk}\nSPD: {speed} CRT: {crt} ACC: {acc}\nInsanity_Ctrl: {Insanity_Ctrl} Reaction_Ctrl: {Reaction_Ctrl} Sp_Atk: {Sp_Atk}");
    }
    
    public void StatCalculate() // 실제로 사용될 스탯 계산 로직
    {
        if (classType == ClassType.Warrior) // 전사 특화 스탯
        {
            max_Hp = STR * 15;
            def = STR * 3;
            atk = STR * 3;
        }
        else
        {
            max_Hp = STR * 10;
            def = STR * 2;
            atk = STR * 2;
        }

        if (classType == ClassType.Assassin) // 도적 특화 스탯
        {
            speed = AGI * 2;
            crt = AGI * 2;
            acc = AGI * 2;
        }
        else
        {
            speed = AGI * 1;
            crt = AGI * 1;
            acc = AGI * 1;
        }

        if (classType == ClassType.Cleric) // 신관 특화 스탯
        {
            Insanity_Ctrl = INT * 2;
            Reaction_Ctrl = INT * 2;
            Sp_Atk = INT * 3;
        }
        else
        {
            Insanity_Ctrl = INT * 1;
            Reaction_Ctrl = INT * 1;
            Sp_Atk = INT * 2;
        }

        current_Hp = max_Hp;
        Debug.Log($"STR: {STR} AGI: {AGI} INT: {INT}\nHP: {max_Hp} DEF: {def} ATK: {atk}\nSPD: {speed} CRT: {crt} ACC: {acc}\nInsanity_Ctrl: {Insanity_Ctrl} Reaction_Ctrl: {Reaction_Ctrl} Sp_Atk: {Sp_Atk}");
    }
}
