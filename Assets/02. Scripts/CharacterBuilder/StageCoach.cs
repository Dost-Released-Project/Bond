
using UnityEngine.AddressableAssets;

public class StageCoach
{
    DataBaseSO professionDb = Addressables.LoadAssetAsync<DataBaseSO>("ClassDataBase").WaitForCompletion();
    DataBaseSO skillDb = Addressables.LoadAssetAsync<DataBaseSO>("SkillDataBase").WaitForCompletion();
    DataBaseSO equipDb = Addressables.LoadAssetAsync<DataBaseSO>("DefaultEquipDataBase").WaitForCompletion();
    
    public BaseCharacter GetRandomCharacter()
    {
        return GetCharacter(professionDb.GetRandom<ClassSO>());
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