#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer.Unity;

/// <summary>
/// 맵 시스템 테스트용 진입점.
/// VContainer EntryPoint로 등록되어 씬 시작 시 맵을 생성하고 표시한다.
/// 에디터 전용 클래스: 프로덕션 빌드에는 포함되지 않는다.
/// </summary>
public class MapTestStarter : IAsyncStartable, IDisposable
{
    private readonly IMapGenerator _generator;
    private readonly IStageLoader _stageLoader;
    private readonly IMapNavigator _mapNavigator;
    private readonly IMapRepository _repository;
    private readonly MapUIController _mapUIController;

    public MapTestStarter(IMapGenerator generator, IStageLoader stageLoader, IMapNavigator mapNavigator,
        IMapRepository repository, MapUIController mapUIController)
    {
        _generator = generator;
        _stageLoader = stageLoader;
        _mapNavigator = mapNavigator;
        _repository = repository;
        _mapUIController = mapUIController;
    }

    public UniTask StartAsync(CancellationToken cancellation = default)
    {
        MapData mapData = _generator.GenerateMap(65732);
        _mapNavigator.Initialize(mapData);
        // 현재 위치 노드 접근
        MapNode current = _mapNavigator.CurrentNode; // 시작 전이면 null

        // 선택 가능한 노드 목록
        List<MapNode> available = _mapNavigator.GetAvailableNodes();

        // 노드 진입 이벤트 구독
        _mapNavigator.OnNodeEntered += OnNodeEntered;
        _mapUIController.ShowMap(mapData);

        return default;
    }

    public void Dispose()
    {
        if (_mapNavigator != null)
            _mapNavigator.OnNodeEntered -= OnNodeEntered;
    }

    public void OnNodeEntered(MapNode node)
    {
        Debug.Log("Entered");
    }
}
#endif
