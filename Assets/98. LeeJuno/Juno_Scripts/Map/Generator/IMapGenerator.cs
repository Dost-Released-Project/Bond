/// <summary>
/// 맵 생성기 인터페이스.
/// 구현체(MapGenerator)는 VContainer를 통해 주입된다.
/// </summary>
public interface IMapGenerator
{
    /// <summary>
    /// 주어진 시드와 챕터 번호로 맵 데이터를 절차적으로 생성한다.
    /// 같은 seed와 actNumber를 넣으면 항상 동일한 맵이 반환된다.
    /// </summary>
    /// <param name="seed">난수 시드 — 맵 재현성 보장</param>
    /// <param name="actNumber">현재 챕터 번호 (향후 챕터별 규칙 분기에 사용)</param>
    public MapData GenerateMap(int seed, int actNumber);
}
