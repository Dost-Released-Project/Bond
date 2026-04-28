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

    public int bonusSTR;
    public int bonusAGI;
    public int bonusINT;

    // [추가] 인벤토리로 되돌릴 때 필요한 원본 아이템 참조
    public BaseItem originItem; 

    public void Upgrade()
    {
        if (upgradeLevel < MAX_UPGRADE)
        {
            upgradeLevel++;
            if (type == EquipmentType.Base)
            {
                bonusSTR += 1;
                bonusAGI += 1;
                bonusINT += 1;
            }
        }
    }

    public Equipment Clone(BaseItem origin)
    {
        return new Equipment
        {
            itemName = this.itemName,
            type = this.type,
            upgradeLevel = this.upgradeLevel,
            bonusSTR = this.bonusSTR,
            bonusAGI = this.bonusAGI,
            bonusINT = this.bonusINT,
            originItem = origin // 원본 기록
        };
    }
}