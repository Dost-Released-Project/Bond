using System.Collections.Generic;

public class StatController
{
    private readonly List<StatModifier> _modifiers = new();

    // public void AddModifier(StatModifier mod) => _modifiers.Add(mod);
    public void AddModifiers(IEnumerable<StatModifier> mods) => _modifiers.AddRange(mods);
    
    // [수정] 소스가 같아도 전체 삭제하지 않고, 딱 '한 번' 등록된 분량만 제거합니다.
    public void RemoveModifiersFromSource(object source, int effectCount)
    {
        // 리스트를 뒤에서부터 순회 (최근에 추가된 것부터 지우기 위함)
        // 지워야 할 효과 개수(effectCount)만큼만 반복하여 삭제합니다.
        int removedCount = 0;
        
        for (int i = _modifiers.Count - 1; i >= 0; i--)
        {
            if (_modifiers[i].source == source)
            {
                _modifiers.RemoveAt(i);
                removedCount++;
                
                // 해당 아이템이 가진 효과 개수만큼 다 지웠다면 루프 종료
                if (removedCount >= effectCount)
                    break;
            }
        }
    }

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