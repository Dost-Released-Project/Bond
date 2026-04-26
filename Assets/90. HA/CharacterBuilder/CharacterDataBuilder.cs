using System;
using System.Linq;
using Bond.Embark;

public partial class CharacterData
{
    public class Builder
    {
        private readonly CharacterData data = new();

        public Builder()
        {
            data.Id = "Missing Id";
            data.ImageAddress = "Missing ImageAddress";
            data.Name = "Outis";
            data.Class = new Class();
            data.Level = 1;
            data.Insanity = 0; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경
            data.RoleType = RoleType.None;
        }
        
        public CharacterData Build() => data;
        
        public Builder SetId(string id)
        {
            data.Id = id;
            return this;
        }

        public Builder SetImageAddress(string address)
        {
            data.ImageAddress = address;
            return this;
        }

        public Builder SetName(string name)
        {
            data.Name = name;
            return this;
        }

        public Builder SetClass(Class @class)
        {
            data.Class = @class;
            return this;
        }

        public Builder SetSkills(SkillBase[] skills)
        {
            data.Skills = skills;
            return this;
        }

        public Builder SetTraits(Trait[] traits)
        {
            data.Traits = traits;
            return this;
        }
        
        public Builder AddRandomTrait()
        {
            Trait randomTrait = new();
            
            int i = Array.FindIndex(data.Traits, trait => trait == null);
            if (i == -1)
            {
                i = UnityEngine.Random.Range(0, data.Traits.Length);
            }
            
            data.Traits[i] = randomTrait;
            
            return this;
        }
        
        public Builder AddRandomSkill()
        {
            SkillBase randomSkill = new TempSkill();
            
            int i = Array.FindIndex(data.Skills, trait => trait == null);
            if (i == -1)
            {
                i = UnityEngine.Random.Range(0, data.Skills.Length);
            }
            
            data.Skills[i] = randomSkill;
            
            return this;
        }
    }
}