using System.IO;
using UnityEngine;

/// <summary>
/// IMapRepository 구현체.
/// Application.persistentDataPath에 JSON 파일로 맵 데이터를 저장/불러온다.
///
/// 주의: MapData의 Dictionary 필드(NodeById, NodesByLayer)는 JsonUtility가 직렬화하지 못하므로
///       Load() 후 BuildLookups()를 호출해 딕셔너리를 재구성한다.
/// </summary>
public class MapRepository : IMapRepository
{
    private const string FileName = "map_save.json";

    // 저장 경로: 플랫폼별 영구 데이터 폴더 (Android, iOS, PC 공통)
    private string FilePath => Path.Combine(Application.persistentDataPath, FileName);

    /// <summary>맵 데이터를 JSON으로 직렬화해 파일에 저장한다.</summary>
    public void Save(MapData mapData)
    {
        string json = JsonUtility.ToJson(mapData, true);
        File.WriteAllText(FilePath, json);
    }

    /// <summary>
    /// 저장된 JSON 파일을 읽어 MapData로 역직렬화한다.
    /// 역직렬화 후 BuildLookups()를 호출해 딕셔너리를 복원한다.
    /// 파일이 없으면 null을 반환한다.
    /// </summary>
    public MapData Load()
    {
        if (HasSave() == false)
            return null;

        string json = File.ReadAllText(FilePath);
        MapData data = JsonUtility.FromJson<MapData>(json);
        data.BuildLookups();
        return data;
    }

    /// <summary>저장 파일이 존재하는지 확인한다.</summary>
    public bool HasSave()
    {
        return File.Exists(FilePath);
    }

    /// <summary>저장 파일을 삭제한다.</summary>
    public void Delete()
    {
        if (HasSave() == false)
            return;

        File.Delete(FilePath);
    }
}
