using System;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class Profession
{
    [JsonProperty] private readonly ClassSO _classData;
    private bool isFirst;

    public Profession(ClassSO classData)
    {
        _classData = classData;
    }

    protected Profession()
    {
    }

    [JsonIgnore] public string Name => _classData.DisplayName;

    public void CalculateStat(Stat stat, BaseCharacterData characterData)
    {
        if (_classData == null) return;
        
        // 0. 기본 능력치
        stat.AGI = _classData.AGI;
        stat.STR = _classData.STR;
        stat.INT = _classData.INT;

        // 1. 장비(무기, 방어구, 장신구) 보너스 합산
        int extraSTR = (characterData.Weapon?.bonusSTR ?? 0) + (characterData.Armor?.bonusSTR ?? 0);
        int extraAGI = (characterData.Weapon?.bonusAGI ?? 0) + (characterData.Armor?.bonusAGI ?? 0);
        int extraINT = (characterData.Weapon?.bonusINT ?? 0) + (characterData.Armor?.bonusINT ?? 0);

        if (characterData.Equips != null)
        {
            foreach (var accItem in characterData.Equips)
            {
                if (accItem == null) continue;
                extraSTR += accItem.bonusSTR;
                extraAGI += accItem.bonusAGI;
                extraINT += accItem.bonusINT;
                // [추후 확장] accItem.ExecuteSpecialEffect(stat);
            }
        }

        // 2. 최종 가용 스탯 결정
        int finalSTR = stat.STR + extraSTR;
        int finalAGI = stat.AGI + extraAGI;
        int finalINT = stat.INT + extraINT;

        // 3. ClassSO의 보정치(Multiplier)를 이용한 최종 계산
        // STR 영향군
        stat.max_Hp = finalSTR * _classData.HP;
        stat.def = finalSTR * _classData.Def;
        stat.atk = finalSTR * _classData.Atk;

        // AGI 영향군
        stat.speed = finalAGI * _classData.Speed;
        stat.crt = finalAGI * _classData.Cri;
        stat.acc = finalAGI * _classData.Acc;

        // INT 영향군
        stat.Insanity_Ctrl = finalINT * _classData.InsanityCtrl;
        stat.Reaction_Ctrl = finalINT * _classData.ReactionCtrl;
        stat.Sp_Atk = finalINT * _classData.SpAtk;

        // 4. 상태 동기화 (최대 체력으로 설정 등)
        stat.current_Hp = stat.max_Hp;

        Debug.Log($"STR: {finalSTR} AGI: {finalAGI} INT: {finalINT}" +
                  $"\nHP: {stat.max_Hp} DEF: {stat.def} ATK: {stat.atk}" +
                  $"\nSPD: {stat.speed} CRT: {stat.crt} ACC: {stat.acc}" +
                  $"\nInsanity_Ctrl: {stat.Insanity_Ctrl} Reaction_Ctrl: {stat.Reaction_Ctrl} Sp_Atk: {stat.Sp_Atk}");
    }
}