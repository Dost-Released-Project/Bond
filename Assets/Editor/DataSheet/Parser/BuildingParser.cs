using System.Collections.Generic;
using System.IO;
using System.Linq;
using CsvHelper;
using CsvHelper.Configuration;
using UnityEditor;
using UnityEngine;

public class BuildingDTO
{
    public string ID { get; set; }
    public string BuildingName { get; set; }
    public BuildingType BuildingType { get; set; }
    public string Description { get; set; }
    public string SpritePath { get; set; }
}

public class BuildingLevelDTO
{
    public string ID { get; set; }
    public int Level { get; set; }
    public int FrontierCost { get; set; }
    public int WoodCost { get; set; }
    public int OreCost { get; set; }
    public int EffectValue { get; set; }
    public int SlotExpansion { get; set; }
    public int FrontierCapAdd { get; set; }
    public int MaterialCapAdd { get; set; }
}

public class BuildingParser : TSVParserBase<BuildingDTO, BuildingData>
{
    public override string TargetFileName => "Building"; // Building.tsv 로드

    protected override string GetAssetName(BuildingDTO dto) => $"BuildingDataSO/BU_{dto.ID}";

    protected override ClassMap<BuildingDTO> GetClassMap() => new BuildingMap();

    private Dictionary<string, List<BuildingLevelData>> _levelCache;

    // 레벨 테이블(BuildingLevel.tsv)을 수동으로 로드하는 메서드
    private void EnsureLevelCacheLoaded()
    {
        if (_levelCache != null) return; // 이미 로드됨

        _levelCache = new Dictionary<string, List<BuildingLevelData>>();
        
        // BuildingLevel.tsv 경로 지정
        string levelPath = Path.Combine(Application.dataPath, "Data/TSV/BuildingLevel.tsv");

        if (!File.Exists(levelPath))
        {
            // 디버깅을 위해 시도한 전체 경로를 출력하도록 개선
            Debug.LogError($"[BuildingParser] 레벨 테이블을 찾을 수 없습니다. 시도한 경로: {levelPath}");
            return;
        }

        // CsvHelper를 이용한 직접 파싱 (TSVParserBase의 로직 참조)
        var config = new CsvConfiguration(System.Globalization.CultureInfo.InvariantCulture)
        {
            Delimiter = "\t",
            Mode = CsvMode.NoEscape,
            HasHeaderRecord = true,
            HeaderValidated = null,
            MissingFieldFound = null,
            PrepareHeaderForMatch = args => args.Header.Trim(),
        };

        // 8행 헤더 스킵 로직 적용 (9번째 줄이 헤더)
        string[] lines = File.ReadAllLines(levelPath);
        var filteredLines = lines.Skip(8).ToList();
        
        // 사용자가 가이드 행을 제외했으므로, 헤더 바로 다음 줄부터 데이터가 시작됨
        // filteredLines.RemoveAt(1); // 이 줄 제거

        string trimmed = string.Join("\n", filteredLines);

        using var reader = new StringReader(trimmed);
        using var csv = new CsvReader(reader, config);
        csv.Context.RegisterClassMap<BuildingLevelMap>();
        var records = csv.GetRecords<BuildingLevelDTO>().ToList();

        foreach (var rec in records)
        {
            if (!_levelCache.ContainsKey(rec.ID))
                _levelCache[rec.ID] = new List<BuildingLevelData>();

            _levelCache[rec.ID].Add(new BuildingLevelData
            {
                level = rec.Level,
                frontierCost = rec.FrontierCost,
                woodCost = rec.WoodCost,
                oreCost = rec.OreCost,
                effectValue = rec.EffectValue,
                slotExpansion = rec.SlotExpansion,
                frontierCapAdd = rec.FrontierCapAdd,
                materialCapAdd = rec.MaterialCapAdd
            });
        }
    }

    protected override void Populate(BuildingData so, BuildingDTO dto)
    {
        // 1. 레벨 데이터 캐시 확인
        EnsureLevelCacheLoaded();

        // 2. 스프라이트 로드
        Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{dto.SpritePath}.png");

        // 3. 해당 건물 ID에 맞는 레벨 리스트 추출
        List<BuildingLevelData> myLevels = _levelCache.ContainsKey(dto.ID) 
            ? _levelCache[dto.ID] 
            : new List<BuildingLevelData>();

        // 4. SO 데이터 주입 (DTO가 아닌 낱개 파라미터 전달)
        so.SetBuildingData(dto.ID, dto.BuildingName, dto.Description, dto.BuildingType, myLevels, sprite);
    }

    protected override void OnPostImport(string outputDir)
    {
        // DB 업데이트 로직 (BuildingDataBaseSO가 있어야 함)
        string subDir = Path.Combine(outputDir, "BuildingDataSO");
        if (!Directory.Exists(subDir)) return;

        var assetPaths = Directory.GetFiles(subDir, "*.asset");
        var assets = new List<BaseSO>();

        foreach (var path in assetPaths)
        {
            var asset = AssetDatabase.LoadAssetAtPath<BuildingData>(path);
            if (asset != null) assets.Add(asset);
        }

        string dbPath = Path.Combine(outputDir, "BuildingDataBase.asset");
        var db = AssetDatabase.LoadAssetAtPath<BuildingDataBaseSO>(dbPath);
        if (db == null)
        {
            db = ScriptableObject.CreateInstance<BuildingDataBaseSO>();
            AssetDatabase.CreateAsset(db, dbPath);
        }

        // 리플렉션으로 _soList 채우기 (팀 공통 방식)
        var field = typeof(DataBaseSO).GetField("_soList", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        field?.SetValue(db, assets);

        EditorUtility.SetDirty(db);
        AssetDatabase.SaveAssets();
        Debug.Log($"[BuildingParser] 건물 DB 통합 완료: {assets.Count}개 등록");
    }
}

public sealed class BuildingMap : ClassMap<BuildingDTO>
{
    public BuildingMap()
    {
        Map(m => m.ID).Name("ID");
        Map(m => m.BuildingName).Name("BuildingName");
        Map(m => m.BuildingType).Name("BuildingType");
        Map(m => m.Description).Name("Description");
        Map(m => m.SpritePath).Name("SpritePath");
    }
}

public sealed class BuildingLevelMap : ClassMap<BuildingLevelDTO>
{
    public BuildingLevelMap()
    {
        Map(m => m.ID).Name("BuildingID");
        Map(m => m.Level).Name("Level");
        Map(m => m.FrontierCost).Name("FrontierCost");
        Map(m => m.WoodCost).Name("WoodCost");
        Map(m => m.OreCost).Name("OreCost");
        Map(m => m.EffectValue).Name("EffectValue");
        Map(m => m.SlotExpansion).Name("SlotExpansion");
        Map(m => m.FrontierCapAdd).Name("FrontierCapAdd");
        Map(m => m.MaterialCapAdd).Name("MaterialCapAdd");
    }
}