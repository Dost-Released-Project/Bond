using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;

public class BaseCharacter : MonoBehaviour, ITurnUseUnit
{
    public Equipment[] equip = new Equipment[2]; // 2개
    public Trait[] traits = new Trait[4]; // 4개
    public Class classType { get; set; }
    public AutoBattle battleType { get; set; }
    public bool isPlayable { get; set; }

    public BaseCharacter sup_Character { get; set; } // 지원 선택 대상. 대상이 행동할 때 역할군에 따른 지원. 탱커: 피격 시 엄호, 서포터: 피격 후 치유, 딜러: 공격 시 지원 공격.

    private Stat stat;

    private void Start()
    {
        stat = GetComponent<Stat>();
        Speed = stat.speed;
    }

    private void Update()
    {
        if (Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            battleType = new AutoBattle_Atk();
            Debug.Log("넌 딜러야.");
        }
        if (Keyboard.current.digit2Key.wasPressedThisFrame)
        {
            battleType = new AutoBattle_Def();
            Debug.Log("넌 탱커야.");
        }
        if (Keyboard.current.digit3Key.wasPressedThisFrame)
        {
            battleType = new AutoBattle_Sup();
            Debug.Log("넌 서포터야.");
        }
        if (Keyboard.current.spaceKey.wasPressedThisFrame)
        {
            if (battleType == null)
            {
                Debug.Log("역할군이 비어있습니다.");
                return;
            }
            
            battleType.BattleAction();
        }

        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            if (battleType == null)
            {
                Debug.Log("역할군이 비어있습니다.");
                return;
            }
            
            isPlayable = !isPlayable;
            battleType.isPlayable = isPlayable;
            
            if (isPlayable)
            {
                Debug.Log("얘는 내가 조종할게");
            }
            else
            {
                Debug.Log("걔는 네가 조종하렴");
            }
        }
        
        if (stat.current_Hp <= 0)
            IsDead = true;
    }
    
    /*
     장비 2종 Equip – Equipment형
     성향 4종 Trait – Trait형
     직업 정보 – 전사 도적 신관 – Class형
     역할군 정보 – 탱거 딜러 서포터 - AutoBattle형
     플블 선택 여부 – bool형
     선택 대상 – 역할군 리액션 발동 대상 지정 – Player형
     ----
     스킬 사용 여부 - and계산(플레이어 위치, 사용 가능 칸, 적 진영 아군 진영)
    */
    
    public int CompareTo(ITurnUseUnit other)
    {
        throw new NotImplementedException();
    }

    public int Speed { get; private set; }
    public bool IsDead { get; private set; }
    public string ImageAddress { get; }
    public int RandomSpeed { get; set; }

    public UniTask TakeTurnAsync()
    {
        throw new NotImplementedException();
    }
}
