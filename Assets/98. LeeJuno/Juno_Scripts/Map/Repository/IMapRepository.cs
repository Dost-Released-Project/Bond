/// <summary>
/// 맵 데이터의 저장/불러오기 인터페이스.
/// 구현체(MapRepository)는 VContainer를 통해 주입된다.
/// </summary>
public interface IMapRepository
{
    /// <summary>맵 데이터를 영구 저장한다. (MapNavigator에서 노드 이동 후 자동 호출)</summary>
    public void Save(MapData mapData);

    /// <summary>
    /// 저장된 맵 데이터를 불러온다.
    /// 불러온 데이터는 BuildLookups()가 호출된 상태다.
    /// 저장 파일이 없으면 null을 반환한다.
    /// </summary>
    public MapData Load();

    /// <summary>저장 파일이 존재하는지 확인한다.</summary>
    public bool HasSave();

    /// <summary>저장 파일을 삭제한다. 새 게임 시작 시 호출한다.</summary>
    public void Delete();
}
