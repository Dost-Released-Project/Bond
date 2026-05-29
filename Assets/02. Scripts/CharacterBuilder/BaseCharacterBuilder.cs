using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public partial class BaseCharacter
{
    public static Dictionary<string, BaseCharacter> Dict = new Dictionary<string, BaseCharacter>();
    
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
        
        private List<string> names = new List<string>(){"Wildboar","GodOfNormalization","MTE","GodOfWar","RiceSummoner"};
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
            chara.SetRole(RandomUtil.GetRandom(RoleType.None));
            chara.CalcStat();
            chara.SetHpFull();
            chara.Id = System.Guid.NewGuid().ToString();
            Dict[chara.Id] = chara;
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
        
        public Builder SetRandomName()
        {
            var name = names.GetRandom();
            SetName(name);
            SetImageAddress(name);
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
            
            chara.IdleImageAddress = data.IdleImageId;
            chara.AttackImageAddress = data.BattleImageId;
            
            var weaponSo = equipDb.GetSO<DefaultEquipSO>(data.DefaultWeaponId);
            var armorSo = equipDb.GetSO<DefaultEquipSO>(data.DefaultArmorId);

            SetWeapon(new Equipment(weaponSo));
            SetArmor(new Equipment(armorSo));

            FillSkill();
            
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
            while (chara.Skills.Any(skill => skill == null))
            {
                AddRandomSkill();
            }
            
            return this;
        }

        // 슬롯 꽉 찬 상태에서 실행 시 랜덤 스킬 교체
        public Builder AddRandomSkill()
        {
            // 빈 슬롯 혹은 랜덤 슬롯 찾기
            // 직업에 맞는 아무 스킬
            SkillBase randomSkill = new SampleSkill(GetAssignableSkills().GetRandom());
            int i = FindEmptyIndexOrRandom();

            chara.Skills[i] = randomSkill;

            return this;
        }

        private SkillData[] GetAssignableSkills()
        {
            // 직업 필터링
            var skills = skillDb.Query<SkillData>((skill) => skill.UseableClasses == chara.Profession.Id);
            Debug.Assert(skills.Count() >= chara.Skills.Length);

            // 이미 있는 스킬 확인
            var already
                = chara.Skills
                .Where(skill => skill != null && skills.Contains(skill.Data))
                .Select(skill => skill.Data);

            // 이미 있는 스킬 제외
            var result = skills.Except(already).ToArray();
            return result;
        }

        private int FindEmptyIndexOrRandom()
        {
            int length = chara.Skills.Length;
            for (int i = 0; i < length; i++)
            {
                if (chara.Skills[i] == null)
                    return i;
            }
            return UnityEngine.Random.Range(0, length);
        }
    }
}