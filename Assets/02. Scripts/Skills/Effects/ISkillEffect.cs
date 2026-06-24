using PipeLine;

namespace Skills.Effects
{
    public enum SkillEffectType
    {
        None = 0,
        HpChange = 1,
        Buff = 2,
        
        체력_감소 = 10,
        체력_회복 = 11,
        버프_스탯_힘 = 20,
        버프_스탯_민첩 = 21,
        버프_스탯_지능 = 22,
        도트 = 30,
    }

    public interface ISkillEffect
    {
        void Execute(BattleContext context, SkillBase skill);
    }
}