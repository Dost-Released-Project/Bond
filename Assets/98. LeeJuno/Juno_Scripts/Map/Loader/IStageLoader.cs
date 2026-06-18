using System;
using Cysharp.Threading.Tasks;

/// <summary>
/// 스테이지 씬 로드/언로드 인터페이스.
/// 구현체(StageLoader)는 VContainer를 통해 주입된다.
/// </summary>
public interface IStageLoader
{
    /// <summary>
    /// 스테이지가 완료되었을 때 발생하는 이벤트.
    /// 스테이지 씬 내부에서 StageLoader.NotifyStageCompleted()를 호출해 발생시킨다.
    /// MapUIController 등에서 구독해 맵으로 복귀하는 처리를 수행한다.
    /// </summary>
    public event Action<StageResult> OnStageCompleted;

    /// <summary>
    /// 현재 씬 로드/언로드가 진행 중인지 여부.
    /// 호출 전에 확인해 이중 호출을 방지할 수 있다.
    /// </summary>
    public bool IsLoading { get; }

    public StageType? CurrentStageType { get; }

    /// <summary>
    /// 지정한 스테이지 타입에 해당하는 씬을 Additive 방식으로 비동기 로드한다.
    /// 이전에 로드된 씬이 있으면 먼저 언로드한 후 새 씬을 로드한다.
    /// </summary>
    public UniTask LoadStage(StageType stageType, MapNode node);

    /// <summary>
    /// 스킬 컷씬을 Additive 방식으로 로드하고 타임라인 재생 완료까지 대기한 뒤 언로드한다.
    /// 반환 시점에 컷씬 로드·재생·언로드가 모두 완료되어 있음이 보장된다(자기완결형).
    /// 전투씬(_currentScene)은 건드리지 않으며 전투씬 로딩 플래그(_isLoading)와도 독립적이다.
    /// </summary>
    public UniTask LoadSkillCutScene(string skillId, string sceneId);

    /// <summary>
    /// 현재 로드된 스테이지 씬을 비동기로 언로드한다.
    /// 로드된 씬이 없으면 아무 동작도 하지 않는다.
    /// </summary>
    public UniTask UnloadCurrentStage();

    /// <summary>
    /// 스테이지 씬 내부에서 결과가 확정되면 호출한다.
    /// OnStageCompleted 이벤트를 발생시켜 맵 복귀 처리를 시작한다.
    /// </summary>
    public void NotifyStageCompleted(StageResult result);
}