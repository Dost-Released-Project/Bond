using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class ClassDTO
{
    public string ID { get; set; }
    public string Name { get; set; }
    public int STR { get; set; }
    public int AGI { get; set; }
    public int INT { get; set; }
    public int HP { get; set; }
    public int DEF { get; set; }
    public int ATK { get; set; }
    public int Speed { get; set; }
    public int CRI { get; set; }
    public int ACC { get; set; }
    public int Eva { get; set; }
    public int ReactionCtrl { get; set; }
    public int SP_ATK { get; set; }
    public string ArmorID { get; set; }
    public string WeaponID { get; set; }
    public string IconID { get; set; }
    public string IdleImageID { get; set; }
    public string BattleImageID { get; set; }
    public string Class { get; set; }
    public int LV { get; set; }
}

public class ClassParser : TSVParserBase<ClassDTO, ClassSO>
{
    public override string TargetFileName => "class";

    protected override string GetAssetName(ClassDTO dto) => $"ClassDataSO/CL_{dto.ID}";

    protected override ClassMap<ClassDTO> GetClassMap() => new ClassMapInstance();

    protected override void Populate(ClassSO so, ClassDTO dto)
    {
        so.SetData(dto.ID, dto.Name, dto.STR, dto.AGI, dto.INT, dto.HP, dto.DEF, dto.ATK, dto.Speed, dto.CRI, dto.ACC, 
            dto.Eva, dto.ReactionCtrl, dto.SP_ATK, dto.ArmorID, dto.WeaponID, dto.IconID, dto.IdleImageID, dto.BattleImageID, dto.Class, dto.LV);
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "ClassDataSO").Replace("\\", "/");
        if (subDir.EndsWith("/")) subDir = subDir.Substring(0, subDir.Length - 1);

        string[] guids = AssetDatabase.FindAssets("t:ClassSO", new[] { subDir });
        var assets = new List<BaseSO>();

        foreach (var guid in guids)
        {
            string path = AssetDatabase.GUIDToAssetPath(guid);
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
        AssetDatabase.SaveAssetIfDirty(db);

        // 어드레서블 자동 등록 추가
        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[ClassParser] 통합 DB 업데이트 완료: {assets.Count}개 등록");
    }

    private sealed class ClassMapInstance : ClassMap<ClassDTO>
    {
        public ClassMapInstance()
        {
            Map(m => m.ID).Name("ID");
            Map(m => m.Name).Name("이름");
            Map(m => m.STR).Name("힘").Default(0);
            Map(m => m.AGI).Name("민첩").Default(0);
            Map(m => m.INT).Name("지능").Default(0);
            Map(m => m.HP).Name("HP").Default(0);
            Map(m => m.DEF).Name("DEF").Default(0);
            Map(m => m.ATK).Name("ATK").Default(0);
            Map(m => m.Speed).Name("SPEED").Default(0); 
            Map(m => m.CRI).Name("CRT").Default(0);
            Map(m => m.ACC).Name("ACC").Default(0);
            Map(m => m.Eva).Name("EVA").Default(0);
            Map(m => m.ReactionCtrl).Name("REACTION_CTRL").Default(0);
            Map(m => m.SP_ATK).Name("SP_ATK").Default(0);
            Map(m => m.ArmorID).Name("방어구 ID");
            Map(m => m.WeaponID).Name("무기 ID");
            Map(m => m.IconID).Name("Icon ID");
            Map(m => m.IdleImageID).Name("IdleImageID");
            Map(m => m.BattleImageID).Name("BattleImageID");
            Map(m => m.Class).Name("Class");
            Map(m => m.LV).Name("LV").Default(0);
        }
    }
}
