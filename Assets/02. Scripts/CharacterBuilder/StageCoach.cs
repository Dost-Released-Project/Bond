
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

        var ch = builder.Build();
        SetRoleAuto(ch);
        
        return ch;
    }

    private void SetRoleAuto(BaseCharacter chara)
    {
        switch (chara.Profession.Id)
        {
            case 0:
                chara.SetRole(RoleType.Tanker);
                break;
            case 1:
            case 2:
                chara.SetRole(RoleType.Dealer);
                break;
            case 3:
                chara.SetRole(RoleType.Supporter);
                break;
            default:
                chara.SetRole(RoleType.None);
                break;
        }
    }
}