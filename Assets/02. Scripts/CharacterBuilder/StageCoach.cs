
using UnityEngine.AddressableAssets;

public class StageCoach
{
    DataBaseSO professionDb = DBSORegistry.LoadSync<ClassDataBaseSO>("ClassDataBase");
    DataBaseSO skillDb = DBSORegistry.LoadSync<SkillDataBaseSO>("SkillDataBase");
    DataBaseSO equipDb = DBSORegistry.LoadSync<DefaultEquipDataBaseSO>("DefaultEquipDataBase");
    
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
            .AddRandomTrait();

        return builder.Build();
    }
}