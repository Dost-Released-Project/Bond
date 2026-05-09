using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class ClassDTO
{
    public string ID { get; set; }
    public string Name { get; set; }
    public int ATK { get; set; }
    public int DEF { get; set; }
    public int CRI { get; set; }
    public int Speed { get; set; }
    public string ArmorID { get; set; }
    public string WeaponID { get; set; }
    public string IconID { get; set; }
}

public class ClassParser : TSVParserBase<ClassDTO, ClassSO>
{
    public override string TargetFileName => "class";

    protected override string GetAssetName(ClassDTO dto) => $"ClassDataSO/CL_{dto.ID}";

    protected override ClassMap<ClassDTO> GetClassMap() => new ClassMapInstance();

    protected override void Populate(ClassSO so, ClassDTO dto)
    {
        so.SetData(dto.ID, dto.Name, dto.ATK, dto.DEF, dto.CRI, dto.Speed, dto.ArmorID, dto.WeaponID, dto.IconID);
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "ClassDataSO");
        if (!Directory.Exists(subDir)) return;

        var assetPaths = Directory.GetFiles(subDir, "*.asset");
        var assets = new List<BaseSO>();

        foreach (var path in assetPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<ClassSO>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "ClassDataBase.asset");
        var db = AssetDatabase.LoadAssetAtPath<ClassDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<ClassDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        // 어드레서블 자동 등록 추가
        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[ClassParser] 통합 DB 업데이트 완료: {assets.Count}개 등록");
    }

    private sealed class ClassMapInstance : ClassMap<ClassDTO>
    {
        public ClassMapInstance()
        {
            Map(m => m.ID).Name("ID");
            Map(m => m.Name).Name("클래스");
            Map(m => m.ATK).Name("ATK").Default(0);
            Map(m => m.DEF).Name("DEF").Default(0);
            Map(m => m.CRI).Name("CRI").Default(0);
            // 시트에 Speed 헤더가 없을 경우를 대비해 선택적 매핑 혹은 기본값 처리
            Map(m => m.Speed).Optional().Default(0); 
            Map(m => m.ArmorID).Name("방어구 ID");
            Map(m => m.WeaponID).Name("무기 ID");
            Map(m => m.IconID).Name("Icon ID");
        }
    }
}
