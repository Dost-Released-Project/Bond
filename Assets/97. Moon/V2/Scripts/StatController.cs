using System.Collections.Generic;

public class StatController
{
    private readonly List<StatModifier> _modifiers = new();

    // public void AddModifier(StatModifier mod) => _modifiers.Add(mod);
    public void AddModifiers(IEnumerable<StatModifier> mods) => _modifiers.AddRange(mods);
    public void RemoveModifiersFromSource(object source) => _modifiers.RemoveAll(m => m.source == source);

    // 특정 스탯 타입에 대한 모든 모디파이어를 적용한 결과 반환
    public float ApplyModifiers(StatType type, float baseValue)
    {
        float flatSum = 0;
        float percentSum = 0;

        foreach (var mod in _modifiers)
        {
            if (mod.type != type) continue;
            if (mod.mode == ModifierMode.Flat) flatSum += mod.value;
            else percentSum += mod.value;
        }

        return (baseValue + flatSum) * (1 + percentSum);
    }
}