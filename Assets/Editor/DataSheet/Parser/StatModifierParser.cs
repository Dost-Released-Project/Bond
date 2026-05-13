using CsvHelper.Configuration;

public class StatModifierDTO
{
    public string ID { get; set; }
    public string Name { get; set; }
    public StatType Type { get; set; }
    public ModifierMode Mode { get; set; }
    public float Value { get; set; }
}

public class StatModifierParser : TSVParserBase<StatModifierDTO, StatModifierDataSO>
{
    public override string TargetFileName => "statModifier";
    protected override string GetAssetName(StatModifierDTO dto) => $"StatModifierDataSO/MOD_{dto.ID}";
    protected override ClassMap<StatModifierDTO> GetClassMap() => new StatModifierMap();

    protected override void Populate(StatModifierDataSO so, StatModifierDTO dto)
    {
        so.modifier = new StatModifier {
            id = dto.ID,
            name = dto.Name,
            type = dto.Type,
            mode = dto.Mode,
            value = dto.Value
        };
    }
}

public sealed class StatModifierMap : ClassMap<StatModifierDTO>
{
    public StatModifierMap() {
        Map(m => m.ID).Name("ID");
        Map(m => m.Name).Name("Name");
        Map(m => m.Type).Name("Type");
        Map(m => m.Mode).Name("Mode");
        Map(m => m.Value).Name("Value");
    }
}