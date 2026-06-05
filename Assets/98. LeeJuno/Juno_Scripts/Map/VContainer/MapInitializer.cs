using System;
using System.Threading;
using Bond.Expedition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 맵 씬 진입 시 Config SO 를 Addressables 로 로드하고, 맵을 생성한 뒤 Config 를 캐시에 보관한다.
/// VContainer EntryPoint(IAsyncStartable)로 등록되어 씬 초기화 시 자동 실행된다.
///
/// 실행 순서:
///   1. IMapRepository.HasSave() 확인 — 저장 데이터가 있으면 2~5 를 건너뜀
///   2. IMapConfigLoader.LoadAsync()       — Config SO 비동기 로드
///   3. MapConfigCache.Set()               — StageLoader 참조용 캐시 저장 (SO 해제 전)
///   4. IMapGenerator.GenerateMap(seed)    — MapData 생성
///   5. IMapNavigator.Initialize(mapData)  — 맵 내비게이터 초기화
///   6. MapUIController.ShowMap(mapData)   — UI 표시
///
/// 주의:
///   IDisposable.Dispose() 는 VContainer 가 씬 언로드 시 자동으로 호출한다.
///   MapConfigCache 가 SO 참조를 보관하는 동안에는 핸들을 해제하면 안 되므로
///   Dispose() 에서 ReleaseConfigs() 를 호출해 핸들 누수를 방지한다.
/// </summary>
public class MapInitializer : IAsyncStartable, IDisposable
{
    private readonly IMapConfigLoader _mapConfigLoader;
    private readonly IMapGenerator _mapGenerator;
    private readonly IMapNavigator _mapNavigator;
    private readonly IMapRepository _mapRepository;
    private readonly MapUIController _mapUIController;
    private readonly MapConfigCache _mapConfigCache;
    private readonly ISpriteLoader _spriteLoader;
    private readonly ExpeditionPayload _expeditionPayload;

    private AsyncOperationHandle<AccessoryDataBaseSO> _accessoryDBHandle;
    private AsyncOperationHandle<Sprite> _bgSpriteHandle;

    /// <summary>
    /// VContainer 생성자 주입.
    /// </summary>
    [Inject]
    public MapInitializer(
        IMapConfigLoader mapConfigLoader,
        IMapGenerator mapGenerator,
        IMapNavigator mapNavigator,
        IMapRepository mapRepository,
        MapUIController mapUIController,
        MapConfigCache mapConfigCache,
        ISpriteLoader spriteLoader,
        ExpeditionPayload expeditionPayload)
    {
        _mapConfigLoader = mapConfigLoader;
        _mapGenerator = mapGenerator;
        _mapNavigator = mapNavigator;
        _mapRepository = mapRepository;
        _mapUIController = mapUIController;
        _mapConfigCache = mapConfigCache;
        _spriteLoader = spriteLoader;
        _expeditionPayload = expeditionPayload;
    }

    /// <summary>
    /// VContainer 가 씬 언로드 시 자동으로 호출한다.
    /// Addressables 핸들을 해제해 메모리 누수를 방지한다.
    /// </summary>
    public void Dispose()
    {
        // MapConfigCache 참조를 먼저 끊은 뒤 Addressables 핸들을 해제한다.
        // Clear() 이전에 ReleaseConfigs() 를 호출하면 SO 참조가 댕글링될 수 있다.
        _mapConfigCache.Clear();
        _mapConfigLoader.ReleaseConfigs();

        // AccessoryDataBase 핸들 해제 — 로드 실패 시 IsValid() 가 false 이므로 안전하게 체크한다
        if (_accessoryDBHandle.IsValid())
            Addressables.Release(_accessoryDBHandle);

        // BattleMapBgChannel 참조 초기화 후 핸들 해제
        BattleMapBgChannel.Clear();
        if (_bgSpriteHandle.IsValid())
            Addressables.Release(_bgSpriteHandle);

        Debug.Log("[MapInitializer] Addressables 핸들 해제 완료.");
    }

    /// <summary>
    /// 씬 진입 시 VContainer 가 자동으로 호출하는 비동기 진입점.
    /// 저장된 맵이 있으면 Config 로드를 건너뛰고 저장 데이터를 복원한다.
    /// </summary>
    public async UniTask StartAsync(CancellationToken cancellation = default)
    {
        // 저장된 맵이 있으면 Config 로드 없이 저장 데이터를 복원한다.
        // if (_mapRepository.HasSave())
        // {
        //     MapData savedData = _mapRepository.Load();
        //     _mapNavigator.Initialize(savedData);
        //     _mapUIController.ShowMap(savedData);
        //     return;
        // }

        // Config SO 비동기 로드 — 씬 언로드 시 토큰이 취소되면 Addressables 작업도 중단된다
        try
        {
            await _mapConfigLoader.LoadAsync(cancellation);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapInitializer] Config 로드 실패로 맵을 생성할 수 없습니다: {e.Message}");
            // TODO: 로비 복귀 또는 재시도 UI 표시
            return;
        }

        MapConfigPackage package = _mapConfigLoader.GetPackage();

        // AccessoryDataBase Addressables 로드 — 실패 시 null 허용, 로그만 출력한다
        AccessoryDataBaseSO accessoryDB = null;
        try
        {
            _accessoryDBHandle = Addressables.LoadAssetAsync<AccessoryDataBaseSO>("AccessoryDataBase");
            accessoryDB = await _accessoryDBHandle;
        }
        catch (Exception e)
        {
            Debug.LogWarning($"[MapInitializer] AccessoryDataBase 로드 실패: {e.Message}");
        }

        // SO 해제 전에 MapGenerator, StageLoader 가 참조할 수 있도록 캐시에 저장한다.
        // MapConfigCache 는 SO 참조를 그대로 보관하므로 ReleaseConfigs() 전에 반드시 호출해야 한다.
        _mapConfigCache.Set(
            package.GeneratorConfig,
            package.StageConfigs,
            package.MonsterGroupConfig,
            package.BossMonsterGroupConfig,
            package.EventConfig,
            package.EventBattleConfig,
            accessoryDB);

        MapData mapData;

        try
        {
            // seed 는 추후 ExpeditionPayload 또는 서버에서 수신하도록 변경한다.
            // 현재는 임시 시드값을 사용한다.
            int seed = UnityEngine.Random.Range(0, int.MaxValue);
            mapData = _mapGenerator.GenerateMap(seed);
            Debug.Log("시드 = " + seed);
            if (mapData == null)
            {
                Debug.LogError("[MapInitializer] GenerateMap() 이 null 을 반환했습니다.");
                return;
            }
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapInitializer] 맵 생성 실패: {e.Message}");
            // TODO: 로비 복귀 또는 재시도 UI 표시
            return;
        }

        // ReleaseConfigs() 는 챕터 종료 시점에 호출한다.
        // MapConfigCache 가 SO 참조를 유지하므로 이 시점에서 해제하지 않는다.

        // DungeonType 에 따른 배경 스프라이트 로드 — 맵 생성 시 한 번만 수행한다.
        // None 이면 로드를 건너뛰고 채널을 비워둔다.
        if (_expeditionPayload.DungeonType != DungeonType.None)
        {
            string bgAddress = $"Map_{_expeditionPayload.DungeonType}";
            _bgSpriteHandle = await _spriteLoader.LoadAsync(bgAddress);

            if (_bgSpriteHandle.Status == AsyncOperationStatus.Succeeded)
            {
                BattleMapBgChannel.Set(_bgSpriteHandle.Result);
            }
            else
            {
                Debug.LogWarning($"[MapInitializer] 배경 스프라이트 로드 실패: address={bgAddress}");
            }
        }

        _mapNavigator.Initialize(mapData);
        _mapUIController.ShowMap(mapData);
    }
}