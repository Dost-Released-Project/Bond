using System;
using Cysharp.Threading.Tasks;
using UnityEngine;
using VContainer;

/// <summary>
/// 맵 UI 전체를 조율하는 컨트롤러.
/// MapNavigator(이동 로직), StageLoader(씬 로드), MapView(렌더링)를 연결한다.
///
/// 역할:
///   - ShowMap()  : 맵 패널을 활성화하고 노드/엣지를 렌더링
///   - HideMap()  : 맵 패널 비활성화 (스테이지 진입 시 호출)
///   - 노드 클릭 → MapNavigator.MoveToNode() → HideMap()
///   - OnNodeEntered → RefreshNodeStates() + StageLoader.LoadStage()
///   - OnStageCompleted → 씬 언로드 → ShowMap() 으로 맵 복귀
///
/// Inspector 연결 필요:
///   _mapView   — MapView 컴포넌트
///   _mapPanel  — 맵 전체 패널 GameObject (SetActive 제어)
/// </summary>
public class MapUIController : MonoBehaviour
{
    [SerializeField] private MapView _mapView;
    [SerializeField] private GameObject _mapPanel;

    private IMapNavigator _navigator;
    private IStageLoader _stageLoader;
    private MapData _cachedMapData;

    /// <summary>VContainer가 의존성을 주입하는 메서드.</summary>
    [Inject]
    public void Construct(IMapNavigator navigator, IStageLoader stageLoader)
    {
        _navigator = navigator;
        _stageLoader = stageLoader;
    }

    private void Start()
    {
        _navigator.OnNodeEntered += OnNodeEntered;
        _stageLoader.OnStageCompleted += HandleStageCompleted;
    }

    private void OnDestroy()
    {
        _navigator.OnNodeEntered -= OnNodeEntered;
        _stageLoader.OnStageCompleted -= HandleStageCompleted;
    }

    /// <summary>
    /// 맵 패널을 열고 지정한 맵 데이터를 렌더링한다.
    /// 새 챕터 시작 또는 스테이지 완료 후 복귀 시 호출한다.
    /// </summary>
    public void ShowMap(MapData mapData)
    {
        _cachedMapData = mapData;
        _mapPanel.SetActive(true);
        _mapView.Initialize(mapData, OnNodeButtonClicked);
    }

    /// <summary>
    /// 맵 패널을 닫는다. 노드 선택 후 스테이지 씬 로드 직전 호출된다.
    /// </summary>
    public void HideMap()
    {
        _mapPanel.SetActive(false);
    }

    /// <summary>
    /// 노드 버튼 클릭 시 MapNavigator에 이동을 요청한다.
    /// 이동에 성공하면 맵을 닫는다. (Available 아닌 노드 클릭은 무시됨)
    /// </summary>
    private void OnNodeButtonClicked(int nodeId)
    {
        bool moved = _navigator.MoveToNode(nodeId);

        if (moved == false)
            return;

        HideMap();
    }

    /// <summary>
    /// MapNavigator.OnNodeEntered 이벤트 핸들러.
    /// 노드 상태 UI를 갱신하고 해당 스테이지 씬을 비동기로 로드한다.
    /// </summary>
    private void OnNodeEntered(MapNode node)
    {
        _mapView.RefreshNodeStates();
        LoadStageAsync(node).Forget();
    }

    /// <summary>
    /// StageLoader.OnStageCompleted 이벤트 핸들러.
    /// 게임 오버 시 별도 처리, 정상 완료 시 씬 언로드 후 맵으로 복귀한다.
    /// </summary>
    private void HandleStageCompleted(StageResult result)
    {
        if (result.IsGameOver)
        {
            // 게임 오버 흐름은 별도 시스템에서 처리 (FlowManager 등)
            return;
        }

        _mapView.RefreshNodeStates();
        UnloadAndShowMapAsync().Forget();
    }

    private async UniTaskVoid LoadStageAsync(MapNode node)
    {
        try
        {
            await _stageLoader.LoadStage(node.StageType, node);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapUIController] 씬 로드 실패: {e.Message}");
        }
    }

    private async UniTaskVoid UnloadAndShowMapAsync()
    {
        try
        {
            await _stageLoader.UnloadCurrentStage();
            ShowMap(_cachedMapData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapUIController] 씬 언로드 실패: {e.Message}");
        }
    }
}
