/// <summary>
/// 이벤트 씬의 진행 단계.
/// Primary: 최초 EventChoice 버튼 표시 단계.
/// Secondary: ItemReward 선택 후 JournalDataSO.Options 기반 2차 버튼 표시 단계.
/// </summary>
public enum EventSceneState
{
    Primary,
    Secondary,
}
