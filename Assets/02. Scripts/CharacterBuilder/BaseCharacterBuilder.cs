using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

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

        private DataBaseSO professionDb = null;
        private DataBaseSO skillDb = null;
        
        public Builder(DataBaseSO proDb, DataBaseSO skillDb)
        {
            chara.Data.Id = "Missing Id";
            chara.Data.ImageAddress = "Missing ImageAddress";
            chara.Data.Name = "Outis";
            chara.Data.Profession = new SampleProfession();
            chara.Data.Level = 0;
            chara.Data.Insanity = 0; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경
            chara.Data.RoleType = RoleType.None;
            
            this.professionDb = proDb;
            this.skillDb = skillDb;
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

        public Builder SetProfession(Profession pro)
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

        public Builder SetRandomProfession()
        {
            var data = professionDb.GetRandom<ClassSO>();
            Profession pro = new Profession(data);
            SetProfession(pro);
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

        public Builder FillSkill()
        {
            // TODO: 실제 직업으로 필터링 하도록 교체
            //skillDb.Query<SkillData>((skill) => skill.UseableClasses == chara.Data.Profession.class)
            var skills = skillDb.Query<SkillData>((s) => true).ToArray();
            Debug.Assert(skills.Count() >= chara.Skills.Length);

            var shuffled = skills.GetShuffled();
            for (int i = 0; i < chara.Skills.Length; i++)
            {
                chara.Skills[i] = new SampleSkill(shuffled[i]);
            }
            
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