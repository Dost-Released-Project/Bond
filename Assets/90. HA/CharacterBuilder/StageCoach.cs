
public class StageCoach
{
    public BaseCharacterData GetRandomCharacter()
    {
        BaseCharacterData.Builder builder = new BaseCharacterData.Builder();
        
        builder
            .AddRandomTrait()
            .AddRandomTrait()
            .AddRandomSkill()
            .AddRandomSkill()
            .AddRandomSkill();
        
        return builder.Build();
    }
}