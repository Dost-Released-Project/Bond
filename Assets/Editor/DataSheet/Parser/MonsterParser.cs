using System.Collections.Generic;
using System.IO;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class MonsterDTO
{
    public string Id           { get; set; }
    public string Name         { get; set; }
    public RoleType RoleType   { get; set; }
    public int Level           { get; set; }
    public int MaxHp           { get; set; }
    public int Atk             { get; set; }
    public float Def           { get; set; }
    public int Speed           { get; set; }
    public float Cri           { get; set; }
    public float Acc           { get; set; }
    public float Eva           { get; set; }
    public int SpAtk           { get; set; }
    public string ImageAddress { get; set; }
    public string IdleImageID  { get; set; }
    public string BattleImageID { get; set; }
    public string SkillIdsRaw     { get; set; }
}

public class MonsterParser : TSVParserBase<MonsterDTO, MonsterSO>
{
    public override string TargetFileName => "MonsterStat";

    // 단일 식 반환이므로 표현식 바디(=>)로 작성
    protected override string GetAssetName(MonsterDTO dto) => $"MonsterDataSO/MN_{dto.Id}";

    protected override ClassMap<MonsterDTO> GetClassMap() => new MonsterDataMap();

    protected override void Populate(MonsterSO so, MonsterDTO dto)
    {
        List<string> skillIds = new List<string>();
        if (string.IsNullOrWhiteSpace(dto.SkillIdsRaw) == false)
        {
            string[] split = dto.SkillIdsRaw.Split(';');
            foreach (string entry in split)
            {
                string trimmed = entry.Trim();
                if (string.IsNullOrWhiteSpace(trimmed) == false)
                {
                    skillIds.Add(trimmed);
                }
            }
        }

        string finalIdleImageId = dto.IdleImageID;
        if (string.IsNullOrWhiteSpace(finalIdleImageId))
        {
            finalIdleImageId = string.IsNullOrWhiteSpace(dto.ImageAddress) ? $"MN_{dto.Id}_Idle" : dto.ImageAddress;
        }

        string finalBattleImageId = dto.BattleImageID;
        if (string.IsNullOrWhiteSpace(finalBattleImageId))
        {
            finalBattleImageId = string.IsNullOrWhiteSpace(dto.ImageAddress) ? $"MN_{dto.Id}_Atk" : dto.ImageAddress;
        }

        Debug.Log($"[MonsterParser] {dto.Name}({dto.Id}) 임포트 완료 - Idle: {finalIdleImageId}, Battle: {finalBattleImageId}");

        so.SetData(
            id:           dto.Id,
            displayName:  dto.Name,
            roleType:     dto.RoleType,
            level:        dto.Level,
            maxHP:        dto.MaxHp,
            atk:          dto.Atk,
            def:          dto.Def,
            speed:        dto.Speed,
            cri:          dto.Cri,
            acc:          dto.Acc,
            eva:          dto.Eva,
            spAtk:        dto.SpAtk,
            imageAddress: dto.ImageAddress,
            idleImageId:  finalIdleImageId,
            battleImageId: finalBattleImageId,
            skillIds:     skillIds
        );
    }

    protected override void OnPostImport(string outputDir)
    {
        string subDir = Path.Combine(outputDir, "MonsterDataSO");
        if (Directory.Exists(subDir) == false) return;

        string[] assetPaths = Directory.GetFiles(subDir, "*.asset");
        List<BaseSO> assets = new List<BaseSO>();

        foreach (string path in assetPaths)
        {
            MonsterSO asset = AssetDatabase.LoadAssetAtPath<MonsterSO>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "MonsterDataBase.asset");
        MonsterDataBaseSO db = AssetDatabase.LoadAssetAtPath<MonsterDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<MonsterDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // DataBaseSO._soList는 private이므로 리플렉션으로 주입 (기존 ClassParser, AccessoryParser 패턴과 동일)
        System.Reflection.FieldInfo field = typeof(DataBaseSO).GetField(
            "_soList",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();

        AddressableHelper.RegisterToAddressable(dbPath);

        Debug.Log($"[MonsterParser] 통합 DB 업데이트 완료: {assets.Count}개 등록");
    }

    private sealed class MonsterDataMap : ClassMap<MonsterDTO>
    {
        public MonsterDataMap()
        {
            Map(m => m.Id).Name("ID");
            Map(m => m.Name).Name("Name");
            Map(m => m.RoleType).Name("RoleType").Default(RoleType.None);
            Map(m => m.Level).Name("Level").Default(0);
            Map(m => m.MaxHp).Name("MaxHP").Default(0);
            Map(m => m.Atk).Name("Atk").Default(0);
            Map(m => m.Def).Name("Def").Default(0f);
            Map(m => m.Speed).Name("Speed").Default(0);
            Map(m => m.Cri).Name("Cri").Default(0f);
            Map(m => m.Acc).Name("Acc").Default(0f);
            Map(m => m.Eva).Name("Eva").Default(0f);
            Map(m => m.SpAtk).Name("SpAtk").Default(0);
            Map(m => m.ImageAddress).Name("ImageAddress").Default("");
            Map(m => m.IdleImageID).Name("IdleImageID").Default("");
            Map(m => m.BattleImageID).Name("BattleImageID").Default("");
            Map(m => m.SkillIdsRaw).Name("SkillIds").Default("");
        }
    }
}
