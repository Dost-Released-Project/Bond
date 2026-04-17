using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer;

/// <summary>
/// IStageLoader 구현체.
/// Addressables를 사용해 스테이지 씬을 Additive 방식으로 비동기 로드/언로드한다.
///
/// 사용 흐름:
///   1. MapNavigator.OnNodeEntered 발생
///   2. MapUIController → LoadStage(stageType, node) 호출
///   3. StageConfig에서 SceneAddress 조회 → Addressables.LoadSceneAsync
///   4. 스테이지 씬 내부 로직이 완료되면 NotifyStageCompleted(result) 호출
///   5. OnStageCompleted 이벤트 → 맵으로 복귀
/// </summary>
public class StageLoader : IStageLoader
{
    private readonly List<StageConfig> _stageConfigs;

    private SceneInstance _currentScene;    // 현재 로드된 씬 인스턴스
    private bool _hasLoadedScene;           // 현재 로드된 씬이 있는지 여부

    [Inject]
    public StageLoader(List<StageConfig> stageConfigs)
    {
        _stageConfigs = stageConfigs;
        _hasLoadedScene = false;
    }

    /// <summary>
    /// 스테이지 완료 시 발생하는 이벤트. 스테이지 씬에서 NotifyStageCompleted()로 발생시킨다.
    /// </summary>
    public event Action<StageResult> OnStageCompleted;

    /// <summary>
    /// 지정한 StageType에 대응하는 씬을 Additive로 비동기 로드한다.
    /// 이미 씬이 로드되어 있으면 언로드 후 새 씬을 로드한다.
    /// StageConfig에 SceneAddress가 설정되어 있지 않으면 로드하지 않는다.
    /// </summary>
    public async UniTask LoadStage(StageType stageType, MapNode node)
    {
        if (_hasLoadedScene)
            await UnloadCurrentStage();

        StageConfig config = FindConfig(stageType);

        if (config == null)
            return;

        AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(
            config.SceneAddress,
            LoadSceneMode.Additive
        );

        _currentScene = await handle.ToUniTask();
        _hasLoadedScene = true;
    }

    /// <summary>
    /// 현재 로드된 스테이지 씬을 언로드한다.
    /// 로드된 씬이 없으면 아무 동작도 하지 않는다.
    /// </summary>
    public async UniTask UnloadCurrentStage()
    {
        if (_hasLoadedScene == false)
            return;

        await Addressables.UnloadSceneAsync(_currentScene).ToUniTask();
        _hasLoadedScene = false;
    }

    /// <summary>
    /// 스테이지 씬 내부에서 결과가 확정되면 이 메서드를 호출한다.
    /// OnStageCompleted 이벤트를 발생시켜 맵 복귀 처리를 시작한다.
    /// </summary>
    public void NotifyStageCompleted(StageResult result)
    {
        OnStageCompleted?.Invoke(result);
    }

    /// <summary>
    /// StageType에 대응하는 StageConfig를 목록에서 찾아 반환한다.
    /// 해당하는 Config가 없으면 null을 반환한다.
    /// </summary>
    private StageConfig FindConfig(StageType stageType)
    {
        foreach (StageConfig config in _stageConfigs)
        {
            if (config.Type == stageType)
                return config;
        }

        return null;
    }
}
