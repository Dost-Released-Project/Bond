
using System;
using System.Collections.Generic;
using UnityEngine.AddressableAssets;

public class StageCoach
{
    private List<string> names = new List<string>(){"Wildboar","GodOfNormalization","MTE","GodOfWar","RiceSummoner"};
    DataBaseSO professionDb = Addressables.LoadAssetAsync<DataBaseSO>("ClassDataBase").WaitForCompletion();
    DataBaseSO skillDb = Addressables.LoadAssetAsync<DataBaseSO>("SkillDataBase").WaitForCompletion();
    DataBaseSO equipDb = Addressables.LoadAssetAsync<DataBaseSO>("DefaultEquipDataBase").WaitForCompletion();
    
    public BaseCharacter GetRandomCharacter()
    {
        BaseCharacter.Builder builder = new BaseCharacter.Builder(professionDb, skillDb, equipDb);

        string name = names.GetRandom();

        builder
            .SetName(name)
            .SetImageAddress(name)
            .SetRandomProfession()
            .FillSkill()
            .AddRandomTrait()
            .AddRandomTrait();
        
        var chara = builder.Build();
        
        chara.SetRole(RandomUtil.GetRandom(RoleType.None));
        
        return chara;
    }
}