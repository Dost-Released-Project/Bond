#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;
using VContainer.Unity;

/// <summary>
/// 맵 시스템 테스트용 진입점.
/// VContainer EntryPoint(IAsyncStartable)로 등록되어 씬 시작 시 맵을 생성하고 표시한다.
/// 에디터 전용 클래스: 프로덕션 빌드에는 포함되지 않는다.
///
/// MapInitializer 와 달리 이 클래스는 단독으로 전체 흐름을 처리한다.
/// MapLifetimeScope 에서 에디터 빌드 시 MapInitializer 를 등록하지 않으므로
/// IMapConfigLoader.LoadAsync() → MapConfigCache.Set() → GenerateMap() 순서를 직접 수행한다.
/// </summary>
public class MapTestStarter : IAsyncStartable, IDisposable
{
    private readonly IMapConfigLoader _mapConfigLoader;
    private readonly MapConfigCache _mapConfigCache;
    private readonly IMapGenerator _generator;
    private readonly IMapNavigator _mapNavigator;
    private readonly IMapRepository _repository;
    private readonly MapUIController _mapUIController;

    [Inject]
    public MapTestStarter(
        IMapConfigLoader mapConfigLoader,
        MapConfigCache mapConfigCache,
        IMapGenerator generator,
        IMapNavigator mapNavigator,
        IMapRepository repository,
        MapUIController mapUIController)
    {
        _mapConfigLoader = mapConfigLoader;
        _mapConfigCache  = mapConfigCache;
        _generator       = generator;
        _mapNavigator    = mapNavigator;
        _repository      = repository;
        _mapUIController = mapUIController;
    }

    /// <summary>
    /// 씬 진입 시 VContainer 가 자동으로 호출하는 비동기 진입점.
    /// Config SO 를 Addressables 로 로드하고, 맵을 생성해 UI 에 표시한다.
    /// </summary>
    public async UniTask StartAsync(CancellationToken cancellation = default)
    {
        // Config SO 비동기 로드
        try
        {
            await _mapConfigLoader.LoadAsync(cancellation);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapTestStarter] Config 로드 실패: {e.Message}");
            return;
        }

        MapConfigPackage package = _mapConfigLoader.GetPackage();

        if (package == null)
        {
            Debug.LogError("[MapTestStarter] GetPackage() 가 null 을 반환했습니다.");
            return;
        }

        // MapGenerator, StageLoader 가 SO 데이터를 참조할 수 있도록 캐시에 저장한다.
        // ReleaseConfigs() 는 챕터 종료 시 호출한다 — 이 시점에서 해제하지 않는다.
        _mapConfigCache.Set(package.GeneratorConfig, package.StageConfigs, package.MonsterGroupConfig, package.EventConfig);

        MapData mapData = _generator.GenerateMap(65732);

        if (mapData == null)
        {
            Debug.LogError("[MapTestStarter] GenerateMap() 이 null 을 반환했습니다.");
            return;
        }

        _mapNavigator.Initialize(mapData);

        // 선택 가능한 노드 목록 확인 (디버그용)
        List<MapNode> available = _mapNavigator.GetAvailableNodes();
        Debug.Log($"[MapTestStarter] 생성 완료. 선택 가능 노드: {available.Count}개");

        // 노드 진입 이벤트 구독
        _mapNavigator.OnNodeEntered += OnNodeEntered;

        _mapUIController.ShowMap(mapData);
    }

    public void Dispose()
    {
        if (_mapNavigator != null)
            _mapNavigator.OnNodeEntered -= OnNodeEntered;
    }

    private void OnNodeEntered(MapNode node)
    {
        Debug.Log($"[MapTestStarter] 노드 진입: {node.StageType} (Layer {node.Layer})");
    }
}
#endif
