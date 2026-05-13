using UnityEngine;

/// <summary>
/// [D] Pure Data: 클래스별 초기 스탯 및 장비를 담는 SO.
/// </summary>
public class DefaultEquipSO : BaseSO
{
    public string ID;
    public ClassType ClassType;
    public int STR, AGI, INT;
    public int UpSTR, UpAGI, UpINT; // 성장 계수 추가
    public string ArmorID, WeaponID, IconID;

    public void SetData(string id, ClassType type, int s, int a, int i, int us, int ua, int ui, string ar, string wp, string ic)
    {
        base.Initialize(id, name, "");
        ClassType = type; 
        STR = s; AGI = a; INT = i;
        UpSTR = us; UpAGI = ua; UpINT = ui;
        ArmorID = ar; WeaponID = wp; IconID = ic;
    }
}

