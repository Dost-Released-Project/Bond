using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class SupplyDTO
{
    public string ID { get; set; }
    public string Name { get; set; }
    public float Rate { get; set; }
    public string ItemID_1 { get; set; }
    public int ItemCount_1 { get; set; }
    public string ItemID_2 { get; set; }
    public int ItemCount_2 { get; set; }
}

public class SupplyParser : TSVParserBase<SupplyDTO, SupplyDataSO>
{
    public override string TargetFileName => "supply";
    protected override string GetAssetName(SupplyDTO dto) => $"SupplyDataSO/SUP_{dto.ID}";
    protected override ClassMap<SupplyDTO> GetClassMap() => new SupplyMap();

    protected override void Populate(SupplyDataSO so, SupplyDTO dto)
    {
        List<SupplyItemPair> items = new List<SupplyItemPair>();

        // 1번 아이템 데이터 유효성 검증 후 리스트 추가
        if (!string.IsNullOrEmpty(dto.ItemID_1) && dto.ItemCount_1 > 0)
        {
            items.Add(new SupplyItemPair { itemId = dto.ItemID_1, count = dto.ItemCount_1 });
        }
        // 2번 아이템 데이터 유효성 검증 후 리스트 추가
        if (!string.IsNullOrEmpty(dto.ItemID_2) && dto.ItemCount_2 > 0)
        {
            items.Add(new SupplyItemPair { itemId = dto.ItemID_2, count = dto.ItemCount_2 });
        }

        so.SetData(dto.ID, dto.Name, dto.Rate, items);
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "SupplyDataSO").Replace("\\", "/");
        if (subDir.EndsWith("/")) subDir = subDir.Substring(0, subDir.Length - 1);

        string[] guids = AssetDatabase.FindAssets("t:SupplyDataSO", new[] { subDir });
        var assets = new List<BaseSO>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
            var asset = AssetDatabase.LoadAssetAtPath<SupplyDataSO>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "SupplyDataBase.asset").Replace("\\", "/");
        var db = AssetDatabase.LoadAssetAtPath<SupplyDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<SupplyDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssetIfDirty(db);

        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[SupplyParser] 통합 보급 DB 업데이트 완료: {assets.Count}개 번들 조립 완료 (경로: {dbPath})");
    }
}

public sealed class SupplyMap : ClassMap<SupplyDTO>
{
    public SupplyMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.Name).Name("Name");
        Map(m => m.Rate).Name("Rate");
        Map(m => m.ItemID_1).Name("ItemID_1");
        Map(m => m.ItemCount_1).Name("ItemCount_1");
        Map(m => m.ItemID_2).Name("ItemID_2");
        Map(m => m.ItemCount_2).Name("ItemCount_2");
    }
}