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
        
        private readonly BaseCharacter chara = new BaseCharacter();

        private DataBaseSO professionDb = null;
        private DataBaseSO skillDb = null;
        private DataBaseSO equipDb = null;
        
        public Builder(
            DataBaseSO proDb,
            DataBaseSO skillDb,
            DataBaseSO equipDb
            )
        {
            chara.Id = "Missing Id";
            chara.ImageAddress = "Missing ImageAddress";
            chara.Name = "Outis";
            chara.Profession = new SampleProfession();
            chara.Level = 0;
            chara.Insanity = 0; // 스트레스(광기) 지수 0~100, Stress는 STR과 혼동될 수 있어서 명칭 변경
            chara.RoleType = RoleType.None;
            
            this.professionDb = proDb;
            this.skillDb = skillDb;
            this.equipDb = equipDb;
        }

        public BaseCharacter Build()
        {
            chara.CalcStat();
            return chara;
        }

        public Builder SetId(string id)
        {
            chara.Id = id;
            return this;
        }

        public Builder SetImageAddress(string address)
        {
            chara.ImageAddress = address;
            return this;
        }

        public Builder SetName(string name)
        {
            chara.Name = name;
            return this;
        }

        public Builder SetWeapon(Equipment weapon)
        {
            chara.Weapon = weapon;
            return this;
        }

        public Builder SetArmor(Equipment armor)
        {
            chara.Armor = armor;
            return this;
        }

        public Builder SetProfession(ClassSO data)
        {
            Profession pro = new Profession(data);
            chara.Profession = pro;
            
            var weaponSo = equipDb.GetSO<DefaultEquipSO>(data.DefaultWeaponId);
            var armorSo = equipDb.GetSO<DefaultEquipSO>(data.DefaultArmorId);

            SetWeapon(new Equipment(weaponSo));
            SetArmor(new Equipment(armorSo));
            
            return this;
        }

        public Builder SetSkills(SkillBase[] skills)
        {
            chara.Skills = skills;
            return this;
        }

        public Builder SetTraits(Trait[] traits)
        {
            chara.Traits = traits;
            return this;
        }

        public Builder SetRandomProfession()
        {
            var data = professionDb.GetRandom<ClassSO>();
            SetProfession(data);
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