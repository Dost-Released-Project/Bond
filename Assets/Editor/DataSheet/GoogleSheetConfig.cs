using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

[CreateAssetMenu(fileName = "GoogleSheetConfig", menuName = "Data/Google Sheet Config")]
public class GoogleSheetConfig : ScriptableObject
{
    public string SpreadsheetId;
    public Entry[] Entries;

    [Serializable]
    public class Entry
    {
        [Tooltip("구글 시트에 저장된 이름이 아닌,\n" +
                 "다운로드 후 저장될 이름\n" +
                 "Parser의 TargetFileName과 일치 해야 함")]
        public string SheetName;
        public string Gid; // 시트 탭 ID
    }
    
    public IEnumerable<GoogleSheetEntry> GetEntries()
    {
        foreach (var entry in Entries)
        {
            yield return new GoogleSheetEntry(SpreadsheetId, entry.SheetName, entry.Gid);
        }
    }
}

[Serializable]
public struct GoogleSheetEntry : IEquatable<GoogleSheetEntry>
{
    public class SheetPageComparer : IEqualityComparer<GoogleSheetEntry>
    {
        public bool Equals(GoogleSheetEntry x, GoogleSheetEntry y)
        {
            return x.SpreadsheetId == y.SpreadsheetId && x.Gid == y.Gid;
        }

        public int GetHashCode(GoogleSheetEntry obj)
        {
            return HashCode.Combine(obj.SpreadsheetId, obj.Gid);
        }
    }
    
    public string SpreadsheetId;
    public string Gid;
    public string SheetName; // 구글 시트에 저장된 이름이 아닌 다운로드 후 저장될 이름

    public GoogleSheetEntry(string spreadsheetId, string sheetName, string gid)
    {
        SpreadsheetId = spreadsheetId;
        SheetName = sheetName;
        Gid = gid;
    }
    
    public string GetExportUrl()
        => $"https://docs.google.com/spreadsheets/d/{SpreadsheetId}/export?format=tsv&gid={Gid}";

    public override string ToString()
    {
        return $"{SpreadsheetId}, {Gid}, {SheetName}";
    }

    public bool Equals(GoogleSheetEntry other)
    {
        return SpreadsheetId == other.SpreadsheetId && Gid == other.Gid && SheetName == other.SheetName;
    }

    public override bool Equals(object obj)
    {
        return obj is GoogleSheetEntry other && Equals(other);
    }

    public override int GetHashCode()
    {
        return HashCode.Combine(SpreadsheetId, Gid, SheetName);
    }
}