using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class AdminTestTool : MonoBehaviour
{
    public string id;
    public static bool isTargetingWeapon = true;
    public BaseCharacter hero;
    
    public static BaseCharacter testHero = null;
    
    private async void Awake()
    {
        hero = BaseCharacter.Sample;
        testHero = hero;
    
        // 1. 데이터베이스들 로드 (어드레서블)
        var classHandle = Addressables.LoadAssetAsync<ClassDataBaseSO>("ClassDataBase");
        var defaultEquipHandle = Addressables.LoadAssetAsync<DefaultEquipDataBaseSO>("DefaultEquipDataBase");

        var classDB = await classHandle.Task;
        var equipDB = await defaultEquipHandle.Task;

        // 2. ID를 기반으로 데이터 가져오기
        var classSO = classDB.GetSO<ClassSO>(id);
        var defaultSO = equipDB.GetSO<DefaultEquipSO>(classSO.DefaultWeaponId);

        if (classSO != null && defaultSO != null)
        {
            // 3. Profession 설정 (보정치용)
            testHero.Profession = new SampleProfession(classSO);
            
            testHero.Stat.AGI = classSO.AGI;
            testHero.Stat.STR = classSO.STR;
            testHero.Stat.INT = classSO.INT;

            // 4. Equipment 객체 생성 및 순수 데이터 주입
            // 요청하신 대로 STR, AGI, INT만 우선 할당합니다.
            testHero.Data.Weapon = new Equipment 
            {
                itemName = "기본 무기", // 임시
                bonusSTR = defaultSO.STR,
                bonusAGI = defaultSO.AGI,
                bonusINT = defaultSO.INT
            };
            
            testHero.Data.Armor = new Equipment 
            {
                itemName = "기본 방어구", // 임시
                bonusSTR = defaultSO.STR,
                bonusAGI = defaultSO.AGI,
                bonusINT = defaultSO.INT
            };

            // 5. 스탯 최종 계산
            testHero.Profession.CalculateStat(testHero.Stat, testHero.Data);
            Debug.Log("임시 캐릭터 생성 완료");
        }
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