using UnityEngine;
using UnityEditor;
using System;
using System.IO;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;

public class TableToSOEditor : EditorWindow
{
    [MenuItem("Settle/Generate All Table SO")]
    public static void GenerateAll()
    {
        // 1. 빌딩 데이터 생성 및 연결
        GenerateBuildingData();
        // 2. 아이템 데이터 생성 (소모품)
        GenerateConsumables();
        // 3. 아이템 데이터 생성 (장신구)
        GenerateAccessories();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>모든 데이터 SO 생성이 완료되었습니다!</color>");
    }

    private static void GenerateBuildingData()
    {
        // 테이블 파싱 (사용자님의 ParseTSV 로직 활용)
        var baseRows = ParseTSVRaw("Building_Base.tsv");
        var levelRows = ParseTSVRaw("Building_Level.tsv");

        Dictionary<string, BuildingData> buildingDict = new Dictionary<string, BuildingData>();

        // [Base] 생성
        foreach (var row in baseRows)
        {
            BuildingData so = CreateOrGetSO<BuildingData>("Buildings", row["ID"]);
            so.id = row["ID"];
            so.buildingName = row["BuildingName"];
            so.buildingType = (BuildingType)Enum.Parse(typeof(BuildingType), row["BuildingType"], true);
            so.description = row["Description"];
            so.buildingSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"{row["SpritePath"]}.png");
            so.levels = new List<BuildingLevelData>();
            
            buildingDict[so.id] = so;
            EditorUtility.SetDirty(so);
        }

        // [Level] 연결
        foreach (var row in levelRows)
        {
            string bId = row["ID"];
            if (buildingDict.TryGetValue(bId, out var so))
            {
                BuildingLevelData level = new BuildingLevelData
                {
                    level = int.Parse(row["Level"]),
                    frontierCost = int.Parse(row["FrontierCost"]),
                    woodCost = int.Parse(row["WoodCost"]),
                    oreCost = int.Parse(row["OreCost"]),
                    effectValue = int.Parse(row["EffectValue"]),
                    slotExpansion = int.Parse(row["SlotExpansion"]),
                    frontierCapAdd = int.Parse(row["FrontierCapAdd"]),
                    materialCapAdd = int.Parse(row["MaterialCapAdd"])
                };
                so.levels.Add(level);
            }
        }
    }

    private static void GenerateConsumables()
    {
        var rows = ParseTSVRaw("Item_Consumable.tsv");
        foreach (var row in rows)
        {
            ConsumableItem so = CreateOrGetSO<ConsumableItem>("Items/Consumables", row["ID"]);
            so.id = row["ID"];
            so.itemName = row["ItemName"];
            so.category = ItemCategory.Consume;
            so.consumableType = (ConsumableType)Enum.Parse(typeof(ConsumableType), row["ConsumableType"], true);
            so.healValue = int.Parse(row["HealValue"]);
            so.totalGlobalMax = int.Parse(row["TotalMax"]);
            so.expeditionSlotMax = int.Parse(row["ExpSlotMax"]);
            so.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{row["IconPath"]}.png");
            EditorUtility.SetDirty(so);
        }
    }

    private static void GenerateAccessories()
    {
        var rows = ParseTSVRaw("Item_Accessory.tsv");
        foreach (var row in rows)
        {
            AccessoryItem so = CreateOrGetSO<AccessoryItem>("Items/Accessories", row["ID"]);
            so.id = row["ID"];
            so.itemName = row["ItemName"];
            so.category = ItemCategory.Accessories;
            so.totalGlobalMax = int.Parse(row["TotalMax"]);
            so.expeditionSlotMax = int.Parse(row["ExpSlotMax"]);
            so.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"{row["IconPath"]}.png");

            // Equipment 데이터 주입
            so.equipmentData = new Equipment
            {
                itemName = so.itemName,
                type = EquipmentType.Accessory,
                bonusSTR = int.Parse(row["BonusSTR"]),
                bonusAGI = int.Parse(row["BonusAGI"]),
                bonusINT = int.Parse(row["BonusINT"]),
                originItem = so
            };
            EditorUtility.SetDirty(so);
        }
    }
    
    // --- 헬퍼 메서드: 대소문자 구분 없는 딕셔너리로 반환 ---
    private static List<Dictionary<string, string>> ParseTSVRaw(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"<color=red>[파일 없음]</color> {path}");
            return new List<Dictionary<string, string>>();
        }

        string[] lines = File.ReadAllLines(path);
        int headerRowIndex = -1;

        // 1. "ID"가 포함된 헤더 줄을 찾습니다.
        for (int i = 0; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] firstTwoCells = lines[i].Split('\t');
            if (firstTwoCells.Length > 0 && firstTwoCells[0].Trim().Equals("ID", StringComparison.OrdinalIgnoreCase))
            {
                headerRowIndex = i;
                break;
            }
        }

        if (headerRowIndex == -1)
        {
            Debug.LogError($"<color=red>[파싱 에러]</color> {fileName}에서 'ID' 헤더를 찾을 수 없습니다.");
            return new List<Dictionary<string, string>>();
        }

        // 2. 헤더 정보를 추출합니다.
        string[] headers = lines[headerRowIndex].Trim().Split('\t');

        var list = new List<Dictionary<string, string>>();

        // 3. 데이터 시작 지점 설정
        // headerRowIndex: 헤더 (ID, Name...)
        // headerRowIndex + 1: 타입 가이드 ((string), (int)...) -> 무시
        // headerRowIndex + 2: 실제 데이터 시작
        int dataStartIndex = headerRowIndex + 2;

        for (int i = dataStartIndex; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] data = lines[i].Split('\t');

            // 데이터 줄의 첫 번째 칸(ID)이 비어있으면 건너뜁니다 (엑셀 빈 칸 방지)
            if (data.Length == 0 || string.IsNullOrWhiteSpace(data[0])) continue;

            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int j = 0; j < headers.Length; j++)
            {
                if (j < data.Length)
                {
                    dict[headers[j].Trim()] = data[j].Trim();
                }
            }

            list.Add(dict);
        }

        Debug.Log($"<color=cyan>[파싱 완료]</color> {fileName} 로드 성공 (데이터: {list.Count}개)");
        return list;
    }

    // --- 헬퍼 메서드: SO 파일 생성 및 로드 ---
    private static T CreateOrGetSO<T>(string folder, string id) where T : ScriptableObject
    {
        string dir = $"Assets/Resources/Data/{folder}";
        if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

        string assetPath = $"{dir}/{id}.asset";
        T asset = AssetDatabase.LoadAssetAtPath<T>(assetPath);
        if (asset == null)
        {
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, assetPath);
        }
        return asset;
    }
}