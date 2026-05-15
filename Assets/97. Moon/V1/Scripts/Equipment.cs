using UnityEngine;
using System;

[Serializable] // 인스펙터에서 수정 가능하게 설정
public enum EquipmentType { Base, Accessory }

[Serializable]
public class Equipment
{
    public string itemName;
    public EquipmentType type;
    public int upgradeLevel = 0;
    public const int MAX_UPGRADE = 5;

    public Equipment(DefaultEquipSO so)
    {
        baseSTR = so.STR;
        baseAGI = so.AGI;
        baseINT = so.INT;

        upSTR = so.UpSTR;
        upAGI = so.UpAGI;
        upINT = so.UpINT;
    }
    
    public Equipment() { }
    
    [Header("Base Stats")]
    public int baseSTR;
    public int baseAGI;
    public int baseINT;

    [Header("Growth Stats (Per Level)")]
    public int upSTR;
    public int upAGI;
    public int upINT;

    // 최종적으로 캐릭터가 참조할 프로퍼티
    public int bonusSTR => baseSTR + (upSTR * upgradeLevel);
    public int bonusAGI => baseAGI + (upAGI * upgradeLevel);
    public int bonusINT => baseINT + (upINT * upgradeLevel);

    public void Upgrade()
    {
        if (upgradeLevel < MAX_UPGRADE)
        {
            upgradeLevel++;
            Debug.Log($"{itemName} 강화 성공! 현재 레벨: {upgradeLevel}");
            // [참고] 업그레이드 시점에 별도의 수치 연산 없이 
            // 위 프로퍼티에서 실시간 계산되므로 데이터 동기화가 간편합니다.
        }
    }
}