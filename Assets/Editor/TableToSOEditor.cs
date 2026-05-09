/* using UnityEngine;
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
        // 1. ë¹Œë”© ?°ì´???ì„± ë°??°ê²°
        GenerateBuildingData();
        // 2. ?„ì´???°ì´???ì„± (?Œëª¨??
        GenerateConsumables();
        // 3. ?„ì´???°ì´???ì„± (?¥ì‹ êµ?
        GenerateAccessories();

        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log("<color=green>ëª¨ë“  ?°ì´??SO ?ì„±???„ë£Œ?˜ì—ˆ?µë‹ˆ??</color>");
    }

    private static void GenerateBuildingData()
    {
        // ?Œì´ë¸??Œì‹± (?¬ìš©?ë‹˜??ParseTSV ë¡œì§ ?œìš©)
        var baseRows = ParseTSVRaw("Building_Base.tsv");
        var levelRows = ParseTSVRaw("Building_Level.tsv");

        Dictionary<string, BuildingData> buildingDict = new Dictionary<string, BuildingData>();

        // [Base] ?ì„±
        foreach (var row in baseRows)
        {
            BuildingData so = CreateOrGetSO<BuildingData>("Buildings", row["ID"]);
            so.id = row["ID"];
            so.buildingName = row["BuildingName"];
            so.buildingType = (BuildingType)Enum.Parse(typeof(BuildingType), row["BuildingType"], true);
            so.description = row["Description"];
            so.buildingSprite = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{row["SpritePath"]}.png");
            so.levels = new List<BuildingLevelData>();
            
            buildingDict[so.id] = so;
            EditorUtility.SetDirty(so);
        }

        // [Level] ?°ê²°
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
            so.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{row["IconPath"]}.png");
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
            so.icon = AssetDatabase.LoadAssetAtPath<Sprite>($"Assets/{row["IconPath"]}.png");

            // Equipment ?°ì´??ì£¼ì…
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
    
    // --- ?¬í¼ ë©”ì„œ?? ?€?Œë¬¸??êµ¬ë¶„ ?†ëŠ” ?•ì…”?ˆë¦¬ë¡?ë°˜í™˜ ---
    private static List<Dictionary<string, string>> ParseTSVRaw(string fileName)
    {
        string path = Path.Combine(Application.streamingAssetsPath, fileName);
        if (!File.Exists(path))
        {
            Debug.LogError($"<color=red>[?Œì¼ ?†ìŒ]</color> {path}");
            return new List<Dictionary<string, string>>();
        }

        string[] lines = File.ReadAllLines(path);
        int headerRowIndex = -1;

        // 1. "ID"ê°€ ?¬í•¨???¤ë” ì¤„ì„ ì°¾ìŠµ?ˆë‹¤.
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
            Debug.LogError($"<color=red>[?Œì‹± ?ëŸ¬]</color> {fileName}?ì„œ 'ID' ?¤ë”ë¥?ì°¾ì„ ???†ìŠµ?ˆë‹¤.");
            return new List<Dictionary<string, string>>();
        }

        // 2. ?¤ë” ?•ë³´ë¥?ì¶”ì¶œ?©ë‹ˆ??
        string[] headers = lines[headerRowIndex].Trim().Split('\t');

        var list = new List<Dictionary<string, string>>();

        // 3. ?°ì´???œì‘ ì§€???¤ì •
        // headerRowIndex: ?¤ë” (ID, Name...)
        // headerRowIndex + 1: ?€??ê°€?´ë“œ ((string), (int)...) -> ë¬´ì‹œ
        // headerRowIndex + 2: ?¤ì œ ?°ì´???œì‘
        int dataStartIndex = headerRowIndex + 2;

        for (int i = dataStartIndex; i < lines.Length; i++)
        {
            if (string.IsNullOrWhiteSpace(lines[i])) continue;

            string[] data = lines[i].Split('\t');

            // ?°ì´??ì¤„ì˜ ì²?ë²ˆì§¸ ì¹?ID)??ë¹„ì–´?ˆìœ¼ë©?ê±´ë„ˆ?ë‹ˆ??(?‘ì? ë¹?ì¹?ë°©ì?)
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

        Debug.Log($"<color=cyan>[?Œì‹± ?„ë£Œ]</color> {fileName} ë¡œë“œ ?±ê³µ (?°ì´?? {list.Count}ê°?");
        return list;
    }

    // --- ?¬í¼ ë©”ì„œ?? SO ?Œì¼ ?ì„± ë°?ë¡œë“œ ---
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
} */
