using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class AccessoryDTO
{
    public string ID { get; set; }
    public string ItemName { get; set; }
    public ItemCategory Category { get; set; }
    public int BonusSTR { get; set; }
    public int BonusAGI { get; set; }
    public int BonusINT { get; set; }
    public int TotalMax { get; set; }
    public int ExpSlotMax { get; set; }
    public string IconPath { get; set; }
}

public class AccessoryParser : TSVParserBase<AccessoryDTO, AccessoryItem>
{
    public override string TargetFileName => "accessory";

    protected override string GetAssetName(AccessoryDTO dto) => $"AccessoryDataSO/AC_{dto.ID}";

    protected override ClassMap<AccessoryDTO> GetClassMap() => new AccessoryMap();

    protected override void Populate(AccessoryItem so, AccessoryDTO dto)
    {
        so.SetBaseData(dto.ID, dto.ItemName, "", dto.Category, dto.TotalMax, dto.ExpSlotMax);
        
        // Accessory 특화 데이터 (Equipment 정보 등)
        if (so.equipmentData == null) so.equipmentData = new Equipment();
        so.equipmentData.itemName = dto.ItemName;
        so.equipmentData.type = EquipmentType.Accessory;
        so.equipmentData.bonusSTR = dto.BonusSTR;
        so.equipmentData.bonusAGI = dto.BonusAGI;
        so.equipmentData.bonusINT = dto.BonusINT;
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "AccessoryDataSO");
        if (!Directory.Exists(subDir)) return;

        var assetPaths = Directory.GetFiles(subDir, "*.asset");
        var assets = new List<BaseSO>();

        foreach (var path in assetPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<AccessoryItem>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "AccessoryDataBase.asset");
        var db = AssetDatabase.LoadAssetAtPath<AccessoryDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<AccessoryDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // DataBaseSO 리플렉션 주입
        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        // 어드레서블 자동 등록 추가
        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[AccessoryParser] 통합 DB 업데이트 완료: {assets.Count}개 등록");
    }
}

public sealed class AccessoryMap : ClassMap<AccessoryDTO>
{
    public AccessoryMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.ItemName).Name("ItemName");
        Map(m => m.Category).Name("Category");
        Map(m => m.BonusSTR).Name("BonusSTR");
        Map(m => m.BonusAGI).Name("BonusAGI");
        Map(m => m.BonusINT).Name("BonusINT");
        Map(m => m.TotalMax).Name("TotalMax");
        Map(m => m.ExpSlotMax).Name("ExpSlotMax");
        Map(m => m.IconPath).Name("IconPath");
    }
}
