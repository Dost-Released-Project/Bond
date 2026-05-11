using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class AdminTestTool : MonoBehaviour
{
    public BaseCharacter hero;
    public static bool isTargetingWeapon = true;
    
    public static BaseCharacter testHero;

    private void Awake()
    {
        hero = BaseCharacter.Sample;
        hero.Profession = new SampleProfession();
        testHero = hero;
    }

    void Update()
    {
        // 1. 캐릭터 데미지 입히기 (식당 테스트용)
        if (Keyboard.current.f1Key.wasPressedThisFrame)
        {
            testHero.ReduceHP(20); 
            Debug.Log($"테스트 캐릭터 HP 감소 {testHero.Stat.current_Hp} / {testHero.Stat.max_Hp}");
        }

        // 2. 스트레스 증가 (여관 테스트용)
        if (Keyboard.current.f2Key.wasPressedThisFrame)
        {
            testHero.ReduceInsanity(20);
            Debug.Log($"테스트 캐릭터 스트레스 증가: {testHero.Insanity}");
        }

        // 3. 캐릭터 선택 (SettlementManager에 전달)
        if (Keyboard.current.f3Key.wasPressedThisFrame)
        {
            FindObjectOfType<SettlementManager>().SelectCharacter(testHero);
            testHero.Profession.CalculateStat(testHero.Stat, testHero.Data);
        }
        
        // 4. 강화 대상 전환 (무기 <-> 방어구)
        if (Keyboard.current.f4Key.wasPressedThisFrame)
        {
            isTargetingWeapon = !isTargetingWeapon;
            string targetName = isTargetingWeapon ? "무기(Weapon)" : "방어구(Armor)";
            Debug.Log($"<color=cyan>[대장간 타겟 변경]</color> 현재 강화 대상: {targetName}");
        }
    }
}