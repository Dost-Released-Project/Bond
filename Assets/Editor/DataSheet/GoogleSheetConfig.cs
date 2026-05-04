using System;
using UnityEngine;

[CreateAssetMenu(fileName = "GoogleSheetConfig", menuName = "Data/Google Sheet Config")]
public class GoogleSheetConfig : ScriptableObject
{
    public string spreadsheetId;
    public SheetEntry[] sheets;

    [Serializable]
    public class SheetEntry
    {
        [Tooltip("저장할 이름")] public string sheetName; // TSV 파일명과 일치해야 함 (= TargetFileName)
        public string gid;       // 시트 탭 ID
    }

    public string GetExportUrl(SheetEntry entry)
        => $"https://docs.google.com/spreadsheets/d/{spreadsheetId}/export?format=tsv&gid={entry.gid}";
}