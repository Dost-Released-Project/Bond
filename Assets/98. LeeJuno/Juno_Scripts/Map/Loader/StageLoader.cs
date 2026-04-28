using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.EventSystems;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.ResourceManagement.ResourceProviders;
using UnityEngine.SceneManagement;
using VContainer;
using Object = UnityEngine.Object;

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
    private readonly MonsterGroupConfig _monsterGroupConfig;
    private readonly Dictionary<StageType, StageConfig> _stageConfigMap;

    private SceneInstance _currentScene;         // 현재 로드된 씬 인스턴스
    private bool _hasLoadedScene;               // 현재 로드된 씬이 있는지 여부
    private bool _isLoading;                    // 비동기 로딩 진행 중 여부 (이중 호출 방지)

    // 스테이지 씬 로드 중 비활성화할 맵 씬 컴포넌트 — 언로드 후 복구
    private AudioListener _mapAudioListener;
    private Camera _mapCamera;
    private EventSystem _mapEventSystem;

    [Inject]
    public StageLoader(List<StageConfig> stageConfigs, MonsterGroupConfig monsterGroupConfig)
    {
        _stageConfigs = stageConfigs;
        _monsterGroupConfig = monsterGroupConfig;
        _hasLoadedScene = false;

        _stageConfigMap = new Dictionary<StageType, StageConfig>();

        if (_stageConfigs != null)
        {
            foreach (StageConfig cfg in _stageConfigs)
            {
                if (cfg != null)
                    _stageConfigMap[cfg.Type] = cfg;
            }
        }
    }

    /// <summary>
    /// 스테이지 완료 시 발생하는 이벤트. 스테이지 씬에서 NotifyStageCompleted()로 발생시킨다.
    /// </summary>
    public event Action<StageResult> OnStageCompleted;

    /// <summary>
    /// 현재 씬 로드/언로드가 진행 중인지 여부.
    /// </summary>
    public bool IsLoading => _isLoading;

    /// <summary>
    /// 지정한 StageType에 대응하는 씬을 Additive로 비동기 로드한다.
    /// 이미 씬이 로드되어 있으면 언로드 후 새 씬을 로드한다.
    /// StageConfig에 SceneAddress가 설정되어 있지 않으면 로드하지 않는다.
    /// Normal 스테이지의 경우 씬 로드 직전 NormalStageContext에 몬스터 정보를 기록한다.
    /// 비동기 실행 중 이중 호출이 들어오면 즉시 반환한다.
    /// </summary>
    public async UniTask LoadStage(StageType stageType, MapNode node)
    {
        // ARCH-02: 이중 호출 방지 — 로딩 진행 중이면 즉시 반환
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            // 맵 씬 컴포넌트를 먼저 캡처하고 비활성화한다.
            // UnloadCurrentStage 이전에 처리해야 복구→재비활성화 왕복을 방지한다.
            DisableMapComponents();

            // 기존 씬 언로드 시 맵 컴포넌트는 복구하지 않는다 (연속 로드 흐름)
            if (_hasLoadedScene)
                await UnloadCurrentStageInternal(restoreMapComponents: false);

            StageConfig config = FindConfig(stageType);

            if (config == null)
            {
                // config가 없으면 씬 로드 없이 반환 — 맵 컴포넌트를 반드시 복구한다
                RestoreMapComponents();
                return;
            }

            // ARCH-04: SetNormalStageContext 직전 잔류 데이터 제거
            if (stageType == StageType.Normal)
            {
                NormalStageContext.Clear();
                SetNormalStageContext(node);
            }

            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(
                config.SceneAddress,
                LoadSceneMode.Additive
            );

            try
            {
                _currentScene = await handle.ToUniTask();
                _hasLoadedScene = true;
            }
            catch (Exception e)
            {
                // 로드 실패 시 맵 컴포넌트 복구
                RestoreMapComponents();
                Debug.LogError($"[StageLoader] 씬 로드 실패: {e.Message}");
                _hasLoadedScene = false;
                throw;
            }
        }
        finally
        {
            _isLoading = false;
        }
    }

    /// <summary>
    /// 현재 로드된 스테이지 씬을 언로드하고 맵 씬 컴포넌트(Camera, AudioListener, EventSystem)를 복구한다.
    /// 로드된 씬이 없으면 아무 동작도 하지 않는다.
    /// </summary>
    public async UniTask UnloadCurrentStage()
    {
        if (_hasLoadedScene == false)
            return;

        await UnloadCurrentStageInternal(restoreMapComponents: true);
    }

    /// <summary>
    /// 씬 언로드 내부 구현.
    /// restoreMapComponents: LoadStage 연속 흐름에서는 false, 독립 호출에서는 true.
    /// </summary>
    private async UniTask UnloadCurrentStageInternal(bool restoreMapComponents)
    {
        try
        {
            await Addressables.UnloadSceneAsync(_currentScene).ToUniTask();
            _hasLoadedScene = false;
        }
        catch (Exception e)
        {
            Debug.LogError($"[StageLoader] 씬 언로드 실패: {e.Message}");
            _hasLoadedScene = false;
            throw;
        }
        finally
        {
            if (restoreMapComponents)
                RestoreMapComponents();
        }
    }

    /// <summary>
    /// 맵 씬의 Camera, AudioListener, EventSystem을 캡처하고 비활성화한다.
    /// Additive 씬 로드 전에 호출해 중복 렌더링·입력·사운드를 방지한다.
    /// </summary>
    private void DisableMapComponents()
    {
        if (_mapCamera == null)
            _mapCamera = Object.FindFirstObjectByType<Camera>();

        if (_mapAudioListener == null)
            _mapAudioListener = Object.FindFirstObjectByType<AudioListener>();

        if (_mapEventSystem == null)
            _mapEventSystem = Object.FindFirstObjectByType<EventSystem>();

        if (_mapCamera != null)
            _mapCamera.enabled = false;

        if (_mapAudioListener != null)
            _mapAudioListener.enabled = false;

        if (_mapEventSystem != null)
            _mapEventSystem.enabled = false;
    }

    /// <summary>
    /// DisableMapComponents()로 비활성화한 맵 씬 컴포넌트를 복구한다.
    /// 씬 언로드 완료 후 또는 로드 실패/조기 반환 시 호출한다.
    /// </summary>
    private void RestoreMapComponents()
    {
        if (_mapCamera != null)
            _mapCamera.enabled = true;

        if (_mapAudioListener != null)
            _mapAudioListener.enabled = true;

        if (_mapEventSystem != null)
            _mapEventSystem.enabled = true;
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
        // 생성자에서 미리 인덱싱한 딕셔너리로 O(1) 조회
        StageConfig result;
        _stageConfigMap.TryGetValue(stageType, out result);
        return result;
    }

    /// <summary>
    /// Normal 스테이지 로드 직전 NormalStageContext에 몬스터 정보를 기록한다.
    /// AssignedMonsterGroupId 가 비어 있거나 그룹을 찾지 못하면 빈 컨텍스트를 기록한다.
    /// </summary>
    private void SetNormalStageContext(MapNode node)
    {
        if (string.IsNullOrEmpty(node.AssignedMonsterGroupId))
        {
            NormalStageContext.Set(string.Empty, new List<string>());
            return;
        }

        MonsterGroupData group = FindMonsterGroup(node.AssignedMonsterGroupId);

        if (group == null)
        {
            NormalStageContext.Set(string.Empty, new List<string>());
            return;
        }

        NormalStageContext.Set(group.Id, group.MonsterIds);
    }

    /// <summary>
    /// Id 로 MonsterGroupData 를 목록에서 찾아 반환한다. 없으면 null.
    /// </summary>
    private MonsterGroupData FindMonsterGroup(string groupId)
    {
        if (_monsterGroupConfig == null || _monsterGroupConfig.Groups == null)
            return null;

        foreach (MonsterGroupData group in _monsterGroupConfig.Groups)
        {
            if (group == null)
                continue;

            if (group.Id == groupId)
                return group;
        }

        return null;
    }
}
