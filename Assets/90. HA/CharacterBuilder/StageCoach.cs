
public class StageCoach
{
    public BaseCharacter GetRandomCharacter()
    {
        BaseCharacter.Builder builder = new BaseCharacter.Builder();
        
        builder
            .SetRandomName()
            .AddRandomTrait()
            .AddRandomTrait()
            .AddRandomSkill()
            .AddRandomSkill()
            .AddRandomSkill();
        
        return builder.Build();
    }
}