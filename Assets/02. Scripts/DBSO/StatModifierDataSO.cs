public class StatModifierDataSO : BaseSO
{
    public StatModifier modifier; // 클래스 데이터 보유
    public void SetData(string id, string modName, StatType type, ModifierMode mode, float value)
    {
        base.Initialize(id, name, "");
        modifier.name = modName;
        modifier.type = type;
        modifier.mode = mode;
        modifier.value = value;
    }
}