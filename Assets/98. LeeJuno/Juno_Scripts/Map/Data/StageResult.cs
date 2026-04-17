using System.Collections.Generic;

/// <summary>
/// 스테이지 종료 시 결과를 담는 데이터 클래스.
/// 스테이지 씬 내부에서 생성해 StageLoader.NotifyStageCompleted()에 전달한다.
/// </summary>
[System.Serializable]
public class StageResult
{
    public bool IsSuccess;          // 스테이지 성공 여부 (전투 승리, 이벤트 선택 완료 등)
    public bool IsGameOver;         // 게임 오버 여부 (전멸 등)
    public List<string> RewardIds;  // 획득한 보상 ID 목록 (아이템 등)
}
/// <summary>
/// 맵 노드에 배치될 수 있는 스테이지 종류.
/// MapGenerator의 가중치 테이블과 배치 규칙에 의해 각 노드에 할당된다.
/// </summary>
public enum StageType
{
    Normal,     // 일반 전투 (가장 흔함)
    Elite,      // 강화 전투 — EliteMinLayer 이상 층에서만 등장
    Boss,       // 보스 전투 — 마지막 층에만 강제 배치
    Camping,    // 휴식 — 보스 직전 층 강제 배치, 그 외 층에서 보조 등장 가능
    Event,      // 랜덤 이벤트 — 선택지 기반 (EventData 참조)
    Shop,       // 상점 — 아이템 구매
}