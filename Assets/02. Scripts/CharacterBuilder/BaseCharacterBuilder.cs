using System;
using System.Collections.Generic;
using System.Linq;
using Bond.Embark;

public partial class BaseCharacter
{
    public class Builder
    {
        // TODO: 나중에 데이터베이스에서 가져오는 걸로 변경
        private List<Trait> traits = new List<Trait>()
        {
            new Trait()
            {
                Name = "A",
                Description = "aaa"
            },
            new Trait()
            {
                Name = "B",
                Description = "bbb"
            },
            new Trait()
            {
                Name = "C",
                Description = "ccc"
            }
        };

        private readonly BaseCharacter chara = Sample;

        public Builder()
        {
            chara.Data.Id = "Missing Id";
            chara.Data.ImageAddress = "Missing ImageAddress";
            chara.Data.Name = "Outis";
            chara.Data.Profession = new SampleProfession();
            chara.Data.Level = 0;
            chara.Data.Insanity = 0; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경
            chara.Data.RoleType = RoleType.None;
        }

        public BaseCharacter Build() => chara;

        public Builder SetId(string id)
        {
            chara.Data.Id = id;
            return this;
        }

        public Builder SetImageAddress(string address)
        {
            chara.Data.ImageAddress = address;
            return this;
        }

        public Builder SetName(string name)
        {
            chara.Data.Name = name;
            return this;
        }

        public Builder SetClass(Profession pro)
        {
            chara.Data.Profession = pro;
            return this;
        }

        public Builder SetSkills(SkillBase[] skills)
        {
            chara.Data.Skills = skills;
            return this;
        }

        public Builder SetTraits(Trait[] traits)
        {
            chara.Data.Traits = traits;
            return this;
        }

        public Builder AddRandomTrait()
        {
            Trait randomTrait = traits.GetRandom();

            int i = Array.FindIndex(chara.Traits, trait => trait == null);
            if (i == -1)
            {
                i = UnityEngine.Random.Range(0, chara.Traits.Length);
            }

            chara.Traits[i] = randomTrait;

            return this;
        }

        public Builder AddRandomSkill()
        {
            SkillBase randomSkill = new SampleSkill();

            int i = Array.FindIndex(chara.Skills, trait => trait == null);
            if (i == -1)
            {
                i = UnityEngine.Random.Range(0, chara.Skills.Length);
            }

            chara.Skills[i] = randomSkill;

            return this;
        }
    }
}