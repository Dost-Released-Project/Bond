using System;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class Profession
{
    [JsonProperty][SerializeField] private ClassSO _classData;
    private bool isFirst;

    public Profession(ClassSO classData)
    {
        _classData = classData;
    }

    protected Profession()
    {
    }

    [JsonIgnore] public string Name => _classData.DisplayName;
    [JsonIgnore] public int Id => int.Parse(_classData.ClassType);
    
    public void CalculateStat(BaseCharacter character, StatController controller)
    {
        if (_classData == null) return;

        Stat stat = character.Stat;
        
        // 0. 기본 능력치
        int baseSTR = _classData.STR;
        int baseAGI = _classData.AGI;
        int baseINT = _classData.INT;
        
        int extraSTR = (character.Weapon?.bonusSTR ?? 0) + (character.Armor?.bonusSTR ?? 0);
        int extraAGI = (character.Weapon?.bonusAGI ?? 0) + (character.Armor?.bonusAGI ?? 0);
        int extraINT = (character.Weapon?.bonusINT ?? 0) + (character.Armor?.bonusINT ?? 0);

        // 1. 모디파이어 적용 (장비 스탯도 여기서 합산 후 적용)
        // 여기서는 기존 장비 코드를 유지하면서 '특수 효과'를 controller에서 가져오는 방식
        stat.STR = controller.ApplyModifiers(StatType.STR, extraSTR + baseSTR);
        stat.AGI = controller.ApplyModifiers(StatType.AGI, extraAGI + baseAGI);
        stat.INT = controller.ApplyModifiers(StatType.INT, extraINT + baseINT);

        // 2. ClassSO의 보정치 적용 및 모디파이어 합산 후 최종 산출
        // STR 영향군
        stat.max_Hp = Mathf.RoundToInt(controller.ApplyModifiers(StatType.MaxHP, stat.STR * _classData.HP));
        stat.atk = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Atk, stat.STR * _classData.Atk));
        stat.def = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Def, stat.STR * _classData.Def));
        
        // AGI 영향군
        stat.speed = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Speed, stat.AGI * _classData.Speed));
        stat.crt = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Cri, stat.AGI * _classData.Cri));
        stat.acc = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Acc, stat.AGI * _classData.Acc));
        
        // INT 영향군
        stat.Insanity_Ctrl =
            Mathf.RoundToInt(controller.ApplyModifiers(StatType.InsanityCtrl, stat.INT * _classData.InsanityCtrl));
        stat.Reaction_Ctrl =
            Mathf.RoundToInt(controller.ApplyModifiers(StatType.ReactionCtrl, stat.INT * _classData.ReactionCtrl));
        stat.Sp_Atk = Mathf.RoundToInt(controller.ApplyModifiers(StatType.SpAtk, stat.INT * _classData.SpAtk));
    }
}