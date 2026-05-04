using CsvHelper.Configuration;

public class ConditionParser : TSVParserBase<Condition, ConditionSO>
{
    public override string TargetFileName    => "conditionsheet";

    protected override ClassMap<Condition> GetClassMap() => new ConditionDataMap();

    protected override string GetAssetName(Condition dto) => $"{dto.Name}";

    protected override void Populate(ConditionSO so, Condition dto)
    {
        so.Id = dto.Id;
        so.Name = dto.Name;
    }
}

public sealed class ConditionDataMap : ClassMap<Condition>
{
    public ConditionDataMap()
    {
        Map(m => m.Id).Name("ID");
        Map(m => m.Name).Name("Trigger_Key");
    }
}