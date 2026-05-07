
using System;
using System.Collections.Generic;

public class StageCoach
{
    private List<string> names = new List<string>(){"Wildboar","GodOfNormalization","MTE","GodOfWar","RiceSummoner"};
    
    public BaseCharacter GetRandomCharacter()
    {
        BaseCharacter.Builder builder = new BaseCharacter.Builder();

        string name = names.GetRandom();
        
        builder
            .SetName(name)
            .SetImageAddress(name)
            .AddRandomTrait()
            .AddRandomTrait()
            .AddRandomSkill()
            .AddRandomSkill()
            .AddRandomSkill();
        
        var chara = builder.Build();
        
        chara.SetRole(RandomUtil.GetRandom(RoleType.None));
        
        return chara;
    }
}