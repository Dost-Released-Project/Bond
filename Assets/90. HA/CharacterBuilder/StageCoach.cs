
public class StageCoach
{
    public CharacterData GetRandomCharacter()
    {
        CharacterData.Builder builder = new CharacterData.Builder();
        
        builder
            .AddRandomTrait()
            .AddRandomTrait()
            .AddRandomSkill()
            .AddRandomSkill()
            .AddRandomSkill();
        
        return builder.Build();
    }
}