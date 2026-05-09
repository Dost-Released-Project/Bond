using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class DefaultEquipDTO
{
    public string ID { get; set; }
    public string ClassType { get; set; }
    public int STR { get; set; }
    public int AGI { get; set; }
    public int INT { get; set; }
    public string ArmorID { get; set; }
    public string WeaponID { get; set; }
    public string IconID { get; set; }
}

public class DefaultEquipParser : TSVParserBase<DefaultEquipDTO, DefaultEquipSO>
{
    public override string TargetFileName => "defaultEquip";

    protected override string GetAssetName(DefaultEquipDTO dto) => $"DefaultEquipDataSO/DE_{dto.ID}";

    protected override ClassMap<DefaultEquipDTO> GetClassMap() => new DefaultEquipMap();

    protected override void Populate(DefaultEquipSO so, DefaultEquipDTO dto)
    {
        so.SetData(dto.ID, dto.ClassType, dto.STR, dto.AGI, dto.INT, dto.ArmorID, dto.WeaponID, dto.IconID);
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "DefaultEquipDataSO");
        if (!Directory.Exists(subDir)) return;

        var assetPaths = Directory.GetFiles(subDir, "*.asset");
        var assets = new List<BaseSO>();

        foreach (var path in assetPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<DefaultEquipSO>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "DefaultEquipDataBase.asset");
        var db = AssetDatabase.LoadAssetAtPath<DefaultEquipDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<DefaultEquipDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[DefaultEquipParser] 통합 DB 업데이트 완료: {assets.Count}개 등록");
    }
}

public sealed class DefaultEquipMap : ClassMap<DefaultEquipDTO>
{
    public DefaultEquipMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.ClassType).Name("ClassType");
        Map(m => m.STR).Name("STR");
        Map(m => m.AGI).Name("AGI");
        Map(m => m.INT).Name("INT");
        Map(m => m.ArmorID).Name("ArmorID");
        Map(m => m.WeaponID).Name("WeaponID");
        Map(m => m.IconID).Name("IconID");
    }
}
