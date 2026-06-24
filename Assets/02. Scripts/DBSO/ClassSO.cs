using UnityEngine;

/// <summary>
/// [D] Pure Data: 클래스(직업)의 성장 능력치를 담는 SO.
/// </summary>
public class ClassSO : BaseSO
{
    public int STR;
    public int AGI;
    public int INT;

    public string DefaultArmorId;
    public string DefaultWeaponId;
    public string IconId;
    public string IdleImageId;
    public string BattleImageId;

    public string ClassType;
    public int LV;

    public void SetData(string id, string name, int str, int agi, int @int, string armor, string weapon, string icon, string idleIcon, string battleIcon, string classType, int lv)
    {
        base.Initialize(id, name, "");
        this.STR = str;
        this.AGI = agi;
        this.INT = @int;
        this.DefaultArmorId = armor;
        this.DefaultWeaponId = weapon;
        this.IconId = icon;
        this.IdleImageId = idleIcon;
        this.BattleImageId = battleIcon;
        this.ClassType = classType;
        this.LV = lv;
    }
}