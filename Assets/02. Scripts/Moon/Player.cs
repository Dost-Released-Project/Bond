using System.Collections.Generic;
using UnityEngine;

public class Player : MonoBehaviour
{
    private string name; // 네임 또는 고유 ID, 플레이어의 정보를 저장하고 한 번 편성된 동료의 신뢰드를 기억하기 위한 키.
    private List<Equipment> equip = new List<Equipment>(); // 2개
    private List<Trait> traits = new List<Trait>(); // 4개
    private Class classType;
    private AutoBattle autoBattle;
    private bool isPlayable;

    private Player sup_Player; // 지원 선택 대상. 대상이 행동할 때 역할군에 따른 지원. 탱커: 피격 시 엄호, 서포터: 피격 후 치유, 딜러: 공격 시 지원 공격.
    
    /*
     장비 2종 Equip – Equipment형
     성향 4종 Trait – Trait형
     직업 정보 – 전사 도적 신관 – Class형
     역할군 정보 – 탱거 딜러 서포터 - AutoBattle형
     플블 선택 여부 – bool형
     ----
     스킬 사용 여부 - and계산(플레이어 위치, 사용 가능 칸, 적 진영 아군 진영)
     선택 대상 – 역할군 리액션 발동 대상 지정 – Player형
    */
}
