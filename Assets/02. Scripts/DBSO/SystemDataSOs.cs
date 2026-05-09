using UnityEngine;

/// <summary>
/// [D] Pure Data: 클래스(직업)의 성장 능력치를 담는 SO.
/// </summary>
public class ClassSO : BaseSO
{
    public int Atk;
    public int Def;
    public int Cri;
    public int Speed;
    
    public string DefaultArmorId;
    public string DefaultWeaponId;
    public string IconId;

    public void SetData(string id, string name, int atk, int def, int cri, int speed, string armor, string weapon, string icon)
    {
        base.Initialize(id, name, "");
        this.Atk = atk;
        this.Def = def;
        this.Cri = cri;
        this.Speed = speed;
        this.DefaultArmorId = armor;
        this.DefaultWeaponId = weapon;
        this.IconId = icon;
    }
}

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
