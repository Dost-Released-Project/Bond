using UnityEngine;

public abstract class Profession
{
    public string Name;

    // 스탯 계산 메인 로직
    public void CalculateStat(Stat stat, BaseCharacterData data)
    {
        // 1. 모든 장비(무기, 방어구, 장신구)의 추가 스탯 합산
        int extraSTR = (data.Weapon?.bonusSTR ?? 0) + (data.Armor?.bonusSTR ?? 0);
        int extraAGI = (data.Weapon?.bonusAGI ?? 0) + (data.Armor?.bonusAGI ?? 0);
        int extraINT = (data.Weapon?.bonusINT ?? 0) + (data.Armor?.bonusINT ?? 0);

        if (data.Equips != null)
        {
            foreach (var acc in data.Equips)
            {
                if (acc == null) continue;
                extraSTR += acc.bonusSTR;
                extraAGI += acc.bonusAGI;
                extraINT += acc.bonusINT;
                // 추후 이곳에 acc.Effect 관련 로직 추가 가능
            }
        }

        // 2. 최종 기초 스탯 확정
        int finalSTR = stat.STR + extraSTR;
        int finalAGI = stat.AGI + extraAGI;
        int finalINT = stat.INT + extraINT;

        // 3. 직업별 가중치 적용 (하드코딩 단계)
        ApplyClassBonus(stat, finalSTR, finalAGI, finalINT);

        // 4. 현재 체력 동기화 (필요 시)
        stat.current_Hp = stat.max_Hp;
        
        Debug.Log($"STR: {finalSTR} AGI: {finalAGI} INT: {finalINT}" +
                  $"\nHP: {stat.max_Hp} DEF: {stat.def} ATK: {stat.atk}" +
                  $"\nSPD: {stat.speed} CRT: {stat.crt} ACC: {stat.acc}" +
                  $"\nInsanity_Ctrl: {stat.Insanity_Ctrl} Reaction_Ctrl: {stat.Reaction_Ctrl} Sp_Atk: {stat.Sp_Atk}");
    }

    // 하위 클래스(Warrior, Assassin 등)에서 가중치만 다르게 구현하거나, 
    // 지금은 내부에서 분기 처리 (우선은 내부 분기로 작성)
    protected void ApplyClassBonus(Stat stat, int str, int agi, int @int)
    {
        stat.max_Hp = str * 15;
        stat.def = str * 3;
        stat.atk = str * 3;
        stat.speed = agi * 1;
        stat.crt = agi * 1;
        stat.acc = agi * 1;
        stat.Insanity_Ctrl = @int * 1;
        stat.Reaction_Ctrl = @int * 1;
        stat.Sp_Atk = @int * 2;
    }
}