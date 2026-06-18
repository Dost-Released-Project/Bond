using System;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.InputSystem;
using UnityEngine.Serialization;

public class AdminTestTool : MonoBehaviour
{
    public string id;
    public BaseCharacter hero;
    
    public BaseCharacter testHero = null;
    
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
        var weaponSO = equipDB.GetSO<DefaultEquipSO>(classSO.DefaultWeaponId);
        var armorSO = equipDB.GetSO<DefaultEquipSO>(classSO.DefaultArmorId);

        if (classSO != null && weaponSO != null && armorSO != null)
        {
            // 3. Profession 설정 (보정치용)
            testHero.Profession = new SampleProfession(classSO);

            // 4. Equipment 객체 생성 및 순수 데이터 주입
            // 요청하신 대로 STR, AGI, INT만 우선 할당합니다.
            testHero.Weapon = new Equipment(weaponSO) 
            {
                itemName = "기본 무기", // 임시
            };
            
            testHero.Armor = new Equipment(armorSO) 
            {
                itemName = "기본 방어구", // 임시
            };

            // 5. 스탯 최종 계산
            testHero.CalcStat();
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
            testHero.IncreaseInsanity(20);
            Debug.Log($"테스트 캐릭터 스트레스 증가: {testHero.Insanity}");
        }

        // // 3. 캐릭터 선택 (SettlementManager에 전달)
        // if (Keyboard.current.f3Key.wasPressedThisFrame)
        // {
        //     FindAnyObjectByType<SettlementManager>().SelectCharacter(testHero);
        //     testHero.CalcStat();
        // }
    }
}