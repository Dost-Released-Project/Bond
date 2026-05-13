using UnityEngine;

/// <summary>
/// [D] Pure Data: 클래스별 초기 스탯 및 장비를 담는 SO.
/// </summary>
public class DefaultEquipSO : BaseSO
{
    public string ClassType;
    public int STR;
    public int AGI;
    public int INT;
    public string ArmorId;
    public string WeaponId;
    public string IconId;

    public void SetData(string id, string classType, int str, int agi, int @int, string armor, string weapon, string icon)
    {
        base.Initialize(id, classType, "");
        this.ClassType = classType;
        this.STR = str;
        this.AGI = agi;
        this.INT = @int;
        this.ArmorId = armor;
        this.WeaponId = weapon;
        this.IconId = icon;
    }
}
