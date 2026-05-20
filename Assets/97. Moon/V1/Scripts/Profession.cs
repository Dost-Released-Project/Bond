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
        stat.AGI = _classData.AGI;
        stat.STR = _classData.STR;
        stat.INT = _classData.INT;
        
        int extraSTR = (character.Weapon?.bonusSTR ?? 0) + (character.Armor?.bonusSTR ?? 0);
        int extraAGI = (character.Weapon?.bonusAGI ?? 0) + (character.Armor?.bonusAGI ?? 0);
        int extraINT = (character.Weapon?.bonusINT ?? 0) + (character.Armor?.bonusINT ?? 0);

        // 1. 모디파이어 적용 (장비 스탯도 여기서 합산 후 적용)
        // 여기서는 기존 장비 코드를 유지하면서 '특수 효과'를 controller에서 가져오는 방식
        float finalSTR = controller.ApplyModifiers(StatType.STR, extraSTR + stat.STR);
        float finalAGI = controller.ApplyModifiers(StatType.AGI, extraAGI + stat.AGI);
        float finalINT = controller.ApplyModifiers(StatType.INT, extraINT + stat.INT);

        // 2. ClassSO의 보정치 적용 및 모디파이어 합산 후 최종 산출
        // STR 영향군
        stat.max_Hp = Mathf.RoundToInt(controller.ApplyModifiers(StatType.MaxHP, finalSTR * _classData.HP));
        stat.atk = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Atk, finalSTR * _classData.Atk));
        stat.def = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Def, finalAGI * _classData.Def));
        
        // AGI 영향군
        stat.speed = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Speed, finalAGI * _classData.Speed));
        stat.crt = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Cri, finalAGI * _classData.Cri));
        stat.acc = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Acc, finalAGI * _classData.Acc));
        
        // INT 영향군
        stat.Insanity_Ctrl =
            Mathf.RoundToInt(controller.ApplyModifiers(StatType.InsanityCtrl, finalINT * _classData.InsanityCtrl));
        stat.Reaction_Ctrl =
            Mathf.RoundToInt(controller.ApplyModifiers(StatType.ReactionCtrl, finalINT * _classData.ReactionCtrl));
        stat.Sp_Atk = Mathf.RoundToInt(controller.ApplyModifiers(StatType.SpAtk, finalINT * _classData.SpAtk));
        
        // 최대 체력이 변동되면 변동 수치만큼 체력 증감. ApplyModifiers로 계산 예정
        // stat.current_Hp = Mathf.Clamp(stat.current_Hp, 0, stat.max_Hp);
        
        Debug.Log($"STR: {finalSTR} AGI: {finalAGI} INT: {finalINT}" + 
                  $"\nHP: {stat.max_Hp} DEF: {stat.def} ATK: {stat.atk}" + 
                  $"\nSPD: {stat.speed} CRT: {stat.crt} ACC: {stat.acc}" + 
                  $"\nInsanity_Ctrl: {stat.Insanity_Ctrl} Reaction_Ctrl: {stat.Reaction_Ctrl} Sp_Atk: {stat.Sp_Atk}");
    }
}