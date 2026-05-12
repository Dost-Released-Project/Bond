using UnityEngine;

public class Profession
{
    private readonly ClassSO _classData;
    private bool isFirst;

    public Profession(ClassSO classData)
    {
        _classData = classData;
    }

    protected Profession()
    {
        throw new System.NotImplementedException();
    }

    public string Name { get; set; }
    
    public void CalculateStat(Stat stat, BaseCharacterData characterData, StatController controller)
    {
        if (_classData == null) return;

        // 1. 순수 베이스 스탯 결정 (Growth)
        float rawSTR = stat.STR;
        float rawAGI = stat.AGI;
        float rawINT = stat.INT;

        if (characterData.Equips != null)
        {
            foreach (var accItem in characterData.Equips)
            {
                if (accItem == null) continue;
                rawSTR += accItem.bonusSTR;
                rawAGI += accItem.bonusAGI;
                rawINT += accItem.bonusINT;
            }
        }

        // 2. 모디파이어 적용 (장비 보너스도 이제 Modifier로 처리하거나, 여기서 합산 후 적용)
        // 여기서는 기존 장비 코드를 유지하면서 '특수 효과'를 controller에서 가져오는 방식
        float finalSTR = controller.ApplyModifiers(StatType.STR, rawSTR + GetEquipmentBonusSTR(characterData));
        float finalAGI = controller.ApplyModifiers(StatType.AGI, rawAGI + GetEquipmentBonusAGI(characterData));
        float finalINT = controller.ApplyModifiers(StatType.INT, rawINT + GetEquipmentBonusINT(characterData));

        // 3. ClassSO의 보정치 적용 및 최종 산출
        stat.max_Hp = Mathf.RoundToInt(controller.ApplyModifiers(StatType.MaxHP, finalSTR * _classData.HP));
        stat.atk = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Atk, finalSTR * _classData.Atk));
        stat.def = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Def, finalAGI * _classData.Def));
        stat.speed = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Speed, finalAGI * _classData.Speed));
        stat.crt = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Cri, finalAGI * _classData.Cri));
        stat.acc = Mathf.RoundToInt(controller.ApplyModifiers(StatType.Acc, finalAGI * _classData.Acc));
        stat.Insanity_Ctrl =
            Mathf.RoundToInt(controller.ApplyModifiers(StatType.InsanityCtrl, finalINT * _classData.InsanityCtrl));
        stat.Reaction_Ctrl =
            Mathf.RoundToInt(controller.ApplyModifiers(StatType.ReactionCtrl, finalINT * _classData.ReactionCtrl));
        stat.Sp_Atk = Mathf.RoundToInt(controller.ApplyModifiers(StatType.SpAtk, finalINT * _classData.SpAtk));
    
        stat.current_Hp = Mathf.Clamp(stat.current_Hp, 0, stat.max_Hp);
        
        Debug.Log($"STR: {finalSTR} AGI: {finalAGI} INT: {finalINT}" + 
                  $"\nHP: {stat.max_Hp} DEF: {stat.def} ATK: {stat.atk}" + 
                  $"\nSPD: {stat.speed} CRT: {stat.crt} ACC: {stat.acc}" + 
                  $"\nInsanity_Ctrl: {stat.Insanity_Ctrl} Reaction_Ctrl: {stat.Reaction_Ctrl} Sp_Atk: {stat.Sp_Atk}");
    }

    // 헬퍼 메소드 예시 (장비 순수 스탯 합산)
    private int GetEquipmentBonusSTR(BaseCharacterData data) => (data.Weapon?.bonusSTR ?? 0) + (data.Armor?.bonusSTR ?? 0);
    private int GetEquipmentBonusAGI(BaseCharacterData data) => (data.Weapon?.bonusAGI ?? 0) + (data.Armor?.bonusAGI ?? 0);
    private int GetEquipmentBonusINT(BaseCharacterData data) => (data.Weapon?.bonusINT ?? 0) + (data.Armor?.bonusINT ?? 0);

    // public void CalculateStat(Stat stat, BaseCharacterData characterData)
    // {
    //     if (_classData == null) return;
    //
    //     // 1. 장비(무기, 방어구, 장신구) 보너스 합산
    //     int extraSTR = (characterData.Weapon?.bonusSTR ?? 0) + (characterData.Armor?.bonusSTR ?? 0);
    //     int extraAGI = (characterData.Weapon?.bonusAGI ?? 0) + (characterData.Armor?.bonusAGI ?? 0);
    //     int extraINT = (characterData.Weapon?.bonusINT ?? 0) + (characterData.Armor?.bonusINT ?? 0);
    //
    //     if (characterData.Equips != null)
    //     {
    //         foreach (var accItem in characterData.Equips)
    //         {
    //             if (accItem == null) continue;
    //             extraSTR += accItem.bonusSTR;
    //             extraAGI += accItem.bonusAGI;
    //             extraINT += accItem.bonusINT;
    //             // [추후 확장] accItem.ExecuteSpecialEffect(stat);
    //         }
    //     }
    //
    //     // 2. 최종 가용 스탯 결정
    //     int finalSTR = stat.STR + extraSTR;
    //     int finalAGI = stat.AGI + extraAGI;
    //     int finalINT = stat.INT + extraINT;
    //
    //     // 3. ClassSO의 보정치(Multiplier)를 이용한 최종 계산
    //     // STR 영향군
    //     stat.max_Hp = finalSTR * _classData.HP;
    //     stat.def = finalSTR * _classData.Def;
    //     stat.atk = finalSTR * _classData.Atk;
    //
    //     // AGI 영향군
    //     stat.speed = finalAGI * _classData.Speed;
    //     stat.crt = finalAGI * _classData.Cri;
    //     stat.acc = finalAGI * _classData.Acc;
    //
    //     // INT 영향군
    //     stat.Insanity_Ctrl = finalINT * _classData.InsanityCtrl;
    //     stat.Reaction_Ctrl = finalINT * _classData.ReactionCtrl;
    //     stat.Sp_Atk = finalINT * _classData.SpAtk;
    //
    //     // 4. 상태 동기화 (최대 체력으로 설정 등)
    //     stat.current_Hp = stat.max_Hp;
    //
    //     Debug.Log($"STR: {finalSTR} AGI: {finalAGI} INT: {finalINT}" +
    //               $"\nHP: {stat.max_Hp} DEF: {stat.def} ATK: {stat.atk}" +
    //               $"\nSPD: {stat.speed} CRT: {stat.crt} ACC: {stat.acc}" +
    //               $"\nInsanity_Ctrl: {stat.Insanity_Ctrl} Reaction_Ctrl: {stat.Reaction_Ctrl} Sp_Atk: {stat.Sp_Atk}");
    // }
}