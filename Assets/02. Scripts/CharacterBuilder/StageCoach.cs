
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

public class StageCoach
{
    DataBaseSO professionDb = Addressables.LoadAssetAsync<DataBaseSO>("ClassDataBase").WaitForCompletion();
    DataBaseSO skillDb = Addressables.LoadAssetAsync<DataBaseSO>("SkillDataBase").WaitForCompletion();
    DataBaseSO equipDb = Addressables.LoadAssetAsync<DataBaseSO>("DefaultEquipDataBase").WaitForCompletion();
    
    public BaseCharacter GetRandomCharacter()
    {
        BaseCharacter.Builder builder = new BaseCharacter.Builder(professionDb, skillDb, equipDb);

        builder
            .SetRandomName()
            .SetRandomProfession()
            .AddRandomTrait()
            .AddRandomTrait();
        
        return builder.Build();
    }

    public BaseCharacter GetCharacter(ClassSO classSO)
    {
        BaseCharacter.Builder builder = new BaseCharacter.Builder(professionDb, skillDb, equipDb);
        
        builder
            .SetRandomName()
            .SetProfession(classSO)
            .AddRandomTrait()
            .AddRandomTrait();
        
        return builder.Build();
    }
}