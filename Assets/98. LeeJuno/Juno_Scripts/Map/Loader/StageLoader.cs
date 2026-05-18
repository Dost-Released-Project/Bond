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
///
/// Config 참조 방식:
///   Inspector SO 직접 주입 방식에서 MapConfigCache 주입 방식으로 변경됐다.
///   MapInitializer.StartAsync() 에서 MapConfigCache.Set() 이 호출된 뒤에
///   LoadStage() 가 호출되므로 MapConfigCache.IsReady 가 보장된다.
/// </summary>
public class StageLoader : IStageLoader
{
    private readonly MapConfigCache _mapConfigCache;
    private readonly IEventContext _eventContext;
    private readonly IStageMonsterContext _stageMonsterContext;
    private readonly Dictionary<StageType, StageConfig> _stageConfigMap;

    private SceneInstance _currentScene;         // 현재 로드된 씬 인스턴스
    private bool _hasLoadedScene;               // 현재 로드된 씬이 있는지 여부
    private bool _isLoading;                    // 비동기 로딩 진행 중 여부 (이중 호출 방지)

    // 스테이지 씬 로드 중 비활성화할 맵 씬 컴포넌트 — 언로드 후 복구
    private AudioListener _mapAudioListener;
    private Camera _mapCamera;
    private EventSystem _mapEventSystem;

    [Inject]
    public StageLoader(MapConfigCache mapConfigCache, IEventContext eventContext, IStageMonsterContext stageMonsterContext)
    {
        _mapConfigCache = mapConfigCache;
        _eventContext = eventContext;
        _stageMonsterContext = stageMonsterContext;
        _hasLoadedScene = false;
        _stageConfigMap = new Dictionary<StageType, StageConfig>();
    }

    /// <summary>
    /// MapConfigCache 가 준비된 시점에 StageConfig 딕셔너리를 구성한다.
    /// LoadStage() 호출 전에 MapInitializer.StartAsync() 가 완료되므로
    /// IsReady 가 보장된 상태에서 호출된다.
    /// </summary>
    private void EnsureStageConfigMap()
    {
        // IsReady 가 false 이면 이전 챕터 데이터를 무효화하고 재구성을 허용한다
        if (_stageConfigMap.Count > 0 && _mapConfigCache.IsReady)
            return;

        _stageConfigMap.Clear();

        if (_mapConfigCache.IsReady == false)
        {
            Debug.LogError("[StageLoader] MapConfigCache 가 아직 준비되지 않았습니다. MapInitializer.StartAsync() 완료 후 호출해야 합니다.");
            return;
        }

        if (_mapConfigCache.StageConfigs == null)
            return;

        foreach (StageConfig cfg in _mapConfigCache.StageConfigs)
        {
            if (cfg != null)
                _stageConfigMap[cfg.Type] = cfg;
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
    /// Normal 스테이지의 경우 씬 로드 직전 IStageMonsterContext에 몬스터 정보를 기록한다.
    /// 비동기 실행 중 이중 호출이 들어오면 즉시 반환한다.
    /// </summary>
    public async UniTask LoadStage(StageType stageType, MapNode node)
    {
        // ARCH-02: 이중 호출 방지 — 로딩 진행 중이면 즉시 반환
        if (_isLoading)
            return;

        if (node == null)
        {
            Debug.LogError("[StageLoader] node 가 null 입니다.");
            return;
        }

        _isLoading = true;
        try
        {
            // MapConfigCache 에서 StageConfig 딕셔너리를 지연 초기화한다.
            // MapInitializer.StartAsync() 완료 후 호출이 보장되므로 IsReady 가 참이다.
            EnsureStageConfigMap();

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
                _stageMonsterContext.Clear();
                SetNormalStageContext(node);
            }
            else if (stageType == StageType.Event)
            {
                _eventContext.Clear();
                SetEventContext(node);
            }

            // 씬 로드 직전 콜백을 채널에 등록한다
            StageCompletionChannel.Register(NotifyStageCompleted);

            AsyncOperationHandle<SceneInstance> handle = Addressables.LoadSceneAsync(config.SceneAddress, LoadSceneMode.Additive);

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
        if (_isLoading)
            return;

        _isLoading = true;
        try
        {
            await UnloadCurrentStageInternal(restoreMapComponents: true);
        }
        finally
        {
            _isLoading = false;
        }
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
            StageCompletionChannel.Unregister();
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

        if (_mapEventSystem == null)
            _mapEventSystem = Object.FindFirstObjectByType<EventSystem>();

        if (_mapCamera != null)
            _mapCamera.gameObject.SetActive(false);

        if (_mapEventSystem != null)
            _mapEventSystem.gameObject.SetActive(false);
    }

    /// <summary>
    /// DisableMapComponents()로 비활성화한 맵 씬 컴포넌트를 복구한다.
    /// 씬 언로드 완료 후 또는 로드 실패/조기 반환 시 호출한다.
    /// </summary>
    private void RestoreMapComponents()
    {
        if (_mapCamera != null)
            _mapCamera.gameObject.SetActive(true);

        if (_mapEventSystem != null)
            _mapEventSystem.gameObject.SetActive(true);
    }

    /// <summary>
    /// 스테이지 씬 내부에서 결과가 확정되면 이 메서드를 호출한다.
    /// IsBattleTriggered 가 true 이면 이벤트 전투 씬으로 전환하고,
    /// 그 외에는 OnStageCompleted 이벤트를 발생시켜 맵 복귀 처리를 시작한다.
    /// </summary>
    public void NotifyStageCompleted(StageResult result)
    {
        if (result.IsBattleTriggered)
        {
            // 이벤트 전투 전환: 이벤트 씬을 언로드하고 전투 씬을 로드한다.
            // UniTask 메서드를 Forget() 으로 실행한다.
            // 람다식: 비동기 흐름을 void 컨텍스트에서 실행하기 위해 사용
            TransitionToEventBattleAsync().Forget();
            return;
        }

        OnStageCompleted?.Invoke(result);
    }

    /// <summary>
    /// 이벤트 전투 전환 비동기 처리.
    /// 현재 이벤트 씬을 언로드하고 EventBattleContext 의 몬스터 정보를 IStageMonsterContext 로 이전한 뒤
    /// EventBattleConfig 에 지정된 전투 씬을 로드한다.
    /// </summary>
    private async UniTask TransitionToEventBattleAsync()
    {
        _isLoading = true;
        try
        {
            EventBattleConfig battleConfig = _mapConfigCache.EventBattleConfig;

            if (battleConfig == null)
            {
                Debug.LogError("[StageLoader] EventBattleConfig 가 캐시에 없습니다.");
                RestoreMapComponents();
                return;
            }

            // 이벤트 씬 언로드 — 맵 컴포넌트는 전투 씬 로드 후에도 복구하지 않는다
            try
            {
                await UnloadCurrentStageInternal(restoreMapComponents: false);
            }
            catch (Exception e)
            {
                // 언로드 실패 시 잔류 컨텍스트를 정리하고 맵 컴포넌트를 복구한 뒤 전환을 중단한다.
                EventBattleContext.Clear();
                RestoreMapComponents();
                Debug.LogError($"[StageLoader] 이벤트 씬 언로드 실패로 전환 중단: {e.Message}");
                return;
            }

            // EventBattleContext 에서 몬스터 정보를 IStageMonsterContext 로 이전
            _stageMonsterContext.Set(EventBattleContext.MonsterGroupId, EventBattleContext.MonsterIds);
            EventBattleContext.Clear();

            // 전투 씬 완료 콜백 재등록 후 씬 로드
            StageCompletionChannel.Register(NotifyStageCompleted);

            AsyncOperationHandle<SceneInstance> handle =
                Addressables.LoadSceneAsync(battleConfig.EventBattleSceneAddress, LoadSceneMode.Additive);

            try
            {
                _currentScene = await handle.ToUniTask();
                _hasLoadedScene = true;
            }
            catch (Exception e)
            {
                RestoreMapComponents();
                Debug.LogError($"[StageLoader] 이벤트 전투 씬 로드 실패: {e.Message}");
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
    /// Normal 스테이지 로드 직전 IStageMonsterContext에 몬스터 정보를 기록한다.
    /// AssignedMonsterGroupId 가 비어 있거나 그룹을 찾지 못하면 빈 컨텍스트를 기록한다.
    /// </summary>
    private void SetNormalStageContext(MapNode node)
    {
        // TODO: 검증 완료 후 제거
        Debug.Log($"[StageLoader] SetNormalStageContext → NodeType={node.GetType().Name}");

        if (string.IsNullOrEmpty(node.AssignedMonsterGroupId))
        {
            _stageMonsterContext.Set(string.Empty, new List<string>());
            return;
        }

        MonsterGroupData group = FindMonsterGroup(node.AssignedMonsterGroupId);

        if (group == null)
        {
            _stageMonsterContext.Set(string.Empty, new List<string>());
            return;
        }

        // TODO: 검증 완료 후 제거
        Debug.Log($"[StageLoader] SetNormalStageContext — group.Id='{group.Id}', MonsterIds={group.MonsterIds.Count}");
        _stageMonsterContext.Set(group.Id, group.MonsterIds);
    }

    /// <summary>
    /// Id 로 MonsterGroupData 를 목록에서 찾아 반환한다. 없으면 null.
    /// MapConfigCache 를 통해 MonsterGroupConfig 에 접근한다.
    /// </summary>
    private MonsterGroupData FindMonsterGroup(string groupId)
    {
        MonsterGroupConfig monsterGroupConfig = _mapConfigCache.MonsterGroupConfig;

        if (monsterGroupConfig == null || monsterGroupConfig.Groups == null)
            return null;

        foreach (MonsterGroupData group in monsterGroupConfig.Groups)
        {
            if (group == null)
                continue;

            if (group.Id == groupId)
                return group;
        }

        return null;
    }

    /// <summary>
    /// Event 스테이지 로드 직전 EventContext 에 이벤트 정보를 기록한다.
    /// AssignedEventId 가 비어 있거나 이벤트를 찾지 못하면 빈 컨텍스트를 기록한다.
    /// </summary>
    private void SetEventContext(MapNode node)
    {
        if (string.IsNullOrEmpty(node.AssignedEventId))
        {
            _eventContext.Set(string.Empty, new List<EventChoice>(), null);
            return;
        }

        EventData ev = FindEventData(node.AssignedEventId);

        if (ev == null)
        {
            _eventContext.Set(string.Empty, new List<EventChoice>(), null);
            return;
        }

        _eventContext.Set(ev.Id, ev.Choices, _mapConfigCache.EventBattleConfig);
    }

    /// <summary>
    /// Id 로 EventData 를 목록에서 찾아 반환한다. 없으면 null.
    /// MapConfigCache 를 통해 EventConfig 에 접근한다.
    /// </summary>
    private EventData FindEventData(string eventId)
    {
        EventConfig eventConfig = _mapConfigCache.EventConfig;

        if (eventConfig == null || eventConfig.Events == null)
            return null;

        foreach (EventData ev in eventConfig.Events)
        {
            if (ev == null)
                continue;

            if (ev.Id == eventId)
                return ev;
        }

        return null;
    }
}
