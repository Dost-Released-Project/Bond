using UnityEngine;

/// <summary>
/// [D] Pure Data: 클래스(직업)의 성장 능력치를 담는 SO.
/// </summary>
public class ClassSO : BaseSO
{
    public int STR;
    public int AGI;
    public int INT;
    public int HP;
    public int Def;
    public int Atk;
    public int Speed;
    public int Cri;
    public int Acc;

    public int InsanityCtrl;
    public int ReactionCtrl;
    public int SpAtk;

    public string DefaultArmorId;
    public string DefaultWeaponId;
    public string IconId;

    public string ClassType;
    public int LV;

    public void SetData(string id, string name, int str, int agi, int @int, int hp, int def, int atk, int speed, int cri, int acc, int insanity, int reaction, int spAtk, string armor, string weapon, string icon, string classType, int lv)
    {
        base.Initialize(id, name, "");
        this.STR = str;
        this.AGI = agi;
        this.INT = @int;
        this.HP = hp;
        this.Def = def;
        this.Atk = atk;
        this.Speed = speed;
        this.Cri = cri;
        this.Acc = acc;
        this.InsanityCtrl = insanity;
        this.ReactionCtrl = reaction;
        this.SpAtk = spAtk;
        this.DefaultArmorId = armor;
        this.DefaultWeaponId = weapon;
        this.IconId = icon;
        this.ClassType = classType;
        this.LV = lv;
    }
}
