using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class ConsumableDTO
{
    public string ID { get; set; }
    public string ItemName { get; set; }
    public ItemCategory Category { get; set; }
    public ConsumableType ConsumableType { get; set; }
    public int HealValue { get; set; }
    public int TotalMax { get; set; }
    public int ExpSlotMax { get; set; }
    public string IconPath { get; set; }
}

public class ConsumableParser : TSVParserBase<ConsumableDTO, ConsumableItem>
{
    public override string TargetFileName => "consumable";

    protected override string GetAssetName(ConsumableDTO dto) => $"ConsumableDataSO/CO_{dto.ID}";

    protected override ClassMap<ConsumableDTO> GetClassMap() => new ConsumableMap();

    protected override void Populate(ConsumableItem so, ConsumableDTO dto)
    {
        so.SetBaseData(dto.ID, dto.ItemName, "", dto.Category, dto.TotalMax, dto.ExpSlotMax);
        so.consumableType = dto.ConsumableType;
        so.healValue = dto.HealValue;
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "ConsumableDataSO");
        if (!Directory.Exists(subDir)) return;

        var assetPaths = Directory.GetFiles(subDir, "*.asset");
        var assets = new List<BaseSO>();

        foreach (var path in assetPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ConsumableItem>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "ConsumableDataBase.asset");
        var db = AssetDatabase.LoadAssetAtPath<ConsumableDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ConsumableDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        // 어드레서블 자동 등록 추가
        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[ConsumableParser] 통합 DB 업데이트 완료: {assets.Count}개 등록");
    }
}

public sealed class ConsumableMap : ClassMap<ConsumableDTO>
{
    public ConsumableMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.ItemName).Name("ItemName");
        Map(m => m.Category).Name("Category");
        Map(m => m.ConsumableType).Name("ConsumableType");
        Map(m => m.HealValue).Name("HealValue");
        Map(m => m.TotalMax).Name("TotalMax");
        Map(m => m.ExpSlotMax).Name("ExpSlotMax");
        Map(m => m.IconPath).Name("IconPath");
    }
}
