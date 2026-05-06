using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;

/// <summary>
/// IMapConfigLoader 구현체.
/// Addressables 로 Config SO 4종을 비동기 로드하고, 맵 생성 완료 후 핸들을 해제한다.
///
/// 핸들 관리 원칙:
///   - 로드한 핸들은 _handles 리스트에 보관한다.
///   - ReleaseConfigs() 에서 모든 핸들을 순회 해제한다.
///   - 로드 실패 시 catch 블록에서 즉시 ReleaseConfigs() 를 호출해 누수를 방지한다.
/// </summary>
public class MapConfigLoader : IMapConfigLoader
{
    private readonly MapConfigLoaderSettings _settings;
    private readonly List<AsyncOperationHandle> _handles;

    private MapConfigPackage _package;
    private bool _isLoaded;

    /// <summary>
    /// VContainer 생성자 주입.
    /// </summary>
    [Inject]
    public MapConfigLoader(MapConfigLoaderSettings settings)
    {
        _settings = settings;
        _handles = new List<AsyncOperationHandle>();
        _isLoaded = false;
    }

    /// <summary>
    /// 모든 Config SO 를 Addressables 로 비동기 로드한다.
    /// 이미 로드된 상태면 경고를 출력하고 즉시 반환한다.
    /// cancellation 이 취소되면 Addressables 로드 작업도 함께 중단된다.
    /// 로드 실패 시 핸들을 즉시 해제하고 예외를 재전파한다.
    /// </summary>
    public async UniTask LoadAsync(CancellationToken cancellation = default)
    {
        if (_isLoaded)
        {
            Debug.LogWarning("[MapConfigLoader] 이미 로드된 상태입니다. ReleaseConfigs() 후 재호출하십시오.");
            return;
        }

        // 주소 유효성 검증
        if (string.IsNullOrEmpty(_settings.MapGeneratorConfigAddress))
        {
            Debug.LogError("[MapConfigLoader] MapGeneratorConfigAddress 가 비어 있습니다.");
            throw new InvalidOperationException("MapGeneratorConfigAddress 가 설정되지 않았습니다.");
        }

        if (string.IsNullOrEmpty(_settings.MonsterGroupConfigAddress))
        {
            Debug.LogError("[MapConfigLoader] MonsterGroupConfigAddress 가 비어 있습니다.");
            throw new InvalidOperationException("MonsterGroupConfigAddress 가 설정되지 않았습니다.");
        }

        if (string.IsNullOrEmpty(_settings.EventConfigAddress))
        {
            Debug.LogError("[MapConfigLoader] EventConfigAddress 가 비어 있습니다.");
            throw new InvalidOperationException("EventConfigAddress 가 설정되지 않았습니다.");
        }

        if (string.IsNullOrEmpty(_settings.EventBattleConfigAddress))
        {
            Debug.LogError("[MapConfigLoader] EventBattleConfigAddress 가 비어 있습니다.");
            throw new InvalidOperationException("EventBattleConfigAddress 가 설정되지 않았습니다.");
        }

        if (_settings.StageConfigAddresses == null || _settings.StageConfigAddresses.Count == 0)
        {
            Debug.LogError("[MapConfigLoader] StageConfigAddresses 가 비어 있습니다.");
            throw new InvalidOperationException("StageConfigAddresses 가 설정되지 않았습니다.");
        }

        _handles.Clear();

        try
        {
            // 각 Config 를 개별 핸들로 로드한다.
            // WhenAll 로 병렬 실행하면 총 대기 시간이 가장 긴 단일 항목과 같아진다.
            // 로드 실패 시 WhenAll 은 첫 번째 예외를 전파하므로 catch 에서 전체 해제한다.
            AsyncOperationHandle<MapGeneratorConfig> generatorHandle =
                Addressables.LoadAssetAsync<MapGeneratorConfig>(_settings.MapGeneratorConfigAddress);

            AsyncOperationHandle<MonsterGroupConfig> monsterHandle =
                Addressables.LoadAssetAsync<MonsterGroupConfig>(_settings.MonsterGroupConfigAddress);

            AsyncOperationHandle<EventConfig> eventHandle =
                Addressables.LoadAssetAsync<EventConfig>(_settings.EventConfigAddress);

            AsyncOperationHandle<EventBattleConfig> eventBattleHandle =
                Addressables.LoadAssetAsync<EventBattleConfig>(_settings.EventBattleConfigAddress);

            _handles.Add(generatorHandle);
            _handles.Add(monsterHandle);
            _handles.Add(eventHandle);
            _handles.Add(eventBattleHandle);

            // StageConfig 는 복수이므로 각 주소별 핸들을 따로 보관한다.
            List<AsyncOperationHandle<StageConfig>> stageHandles = new List<AsyncOperationHandle<StageConfig>>();

            foreach (string address in _settings.StageConfigAddresses)
            {
                AsyncOperationHandle<StageConfig> stageHandle =
                    Addressables.LoadAssetAsync<StageConfig>(address);

                stageHandles.Add(stageHandle);
                _handles.Add(stageHandle);
            }

            // 병렬 대기 — 4개 Config 를 동시에 로드하며 취소 토큰을 전달한다
            await UniTask.WhenAll(
                generatorHandle.ToUniTask(cancellationToken: cancellation),
                monsterHandle.ToUniTask(cancellationToken: cancellation),
                eventHandle.ToUniTask(cancellationToken: cancellation),
                eventBattleHandle.ToUniTask(cancellationToken: cancellation)
            );

            // StageConfig 핸들 목록 인덱스 순서가 이미 주소 순서와 동일하므로
            // 병렬 WhenAll 후 결과 배열 인덱스로 순서를 보장한다.
            UniTask<StageConfig>[] stageTasks = new UniTask<StageConfig>[stageHandles.Count];
            for (int i = 0; i < stageHandles.Count; i++)
                stageTasks[i] = stageHandles[i].ToUniTask(cancellationToken: cancellation);

            StageConfig[] stageResults = await UniTask.WhenAll(stageTasks);
            List<StageConfig> stageConfigs = new List<StageConfig>(stageResults);

            _package = new MapConfigPackage(
                generatorConfig:    generatorHandle.Result,
                stageConfigs:       stageConfigs,
                monsterGroupConfig: monsterHandle.Result,
                eventConfig:        eventHandle.Result,
                eventBattleConfig:  eventBattleHandle.Result
            );

            _isLoaded = true;
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapConfigLoader] Config 로드 실패: {e.Message}");
            // 로드 실패 시 핸들 누수 방지를 위해 즉시 해제한다.
            ReleaseConfigs();
            throw;
        }
    }

    /// <summary>
    /// 로드된 Config 들을 MapGenerator 에 전달 가능한 패키지로 반환한다.
    /// LoadAsync() 완료 전에 호출하면 null 을 반환하고 에러 로그를 출력한다.
    /// </summary>
    public MapConfigPackage GetPackage()
    {
        if (_isLoaded == false)
        {
            Debug.LogError("[MapConfigLoader] LoadAsync() 가 완료되지 않았습니다.");
            return null;
        }

        return _package;
    }

    /// <summary>
    /// 모든 AsyncOperationHandle 을 해제한다.
    /// handle.IsValid() 확인 후 해제해 partial 실패 시 이중 해제를 방지한다.
    /// </summary>
    public void ReleaseConfigs()
    {
        foreach (AsyncOperationHandle handle in _handles)
        {
            if (handle.IsValid())
                Addressables.Release(handle);
        }

        _handles.Clear();
        _package = null;
        _isLoaded = false;
    }
}
