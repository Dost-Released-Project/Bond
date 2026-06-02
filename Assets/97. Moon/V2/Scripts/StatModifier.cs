using System;

[Serializable]
public enum StatType
{
    STR, AGI, INT, 
    DamageMultiplier,      // 데미지 증가 (비율)
    DamageReduction,       // 받는 데미지 감소 (비율)
    HealEfficiency,        // 체력 회복량 상승 (비율)
    StressResistance,      // 스트레스 상승량 감소 (비율)
    MaxHP,
    Atk,
    Def,
    Speed,
    Cri,
    Acc,
    Eva,
    ReactionCtrl,
    SpAtk
}

[Serializable]
public enum ModifierMode { Flat, Percent } // 고정치 합산 vs 비율 곱산

[Serializable]
public class StatModifier
{
    public string id;
    public string name;
    public StatType type;
    public ModifierMode mode;
    public float value;
    public object source; // 이 효과가 어디서 왔는지(장신구, 버프 등) 식별자
}