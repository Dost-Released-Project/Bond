using System;
using Bond.WT.Journal;
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
    private ISpriteLoader _spriteLoader;
    private JournalSystem _journalSystem;
    private JournalModel _journalModel;
    private EventLogAccumulator _logAccumulator;
    private MapData _cachedMapData;

    /// <summary>VContainer가 의존성을 주입하는 메서드.</summary>
    [Inject]
    public void Construct(
        IMapNavigator navigator,
        IStageLoader stageLoader,
        ISpriteLoader spriteLoader,
        JournalSystem journalSystem,
        JournalModel journalModel,
        EventLogAccumulator logAccumulator)
    {
        _navigator = navigator;
        _stageLoader = stageLoader;
        _spriteLoader = spriteLoader;
        _journalSystem = journalSystem;
        _journalModel = journalModel;
        _logAccumulator = logAccumulator;
        _navigator.OnNodeEntered += OnNodeEntered;
        _stageLoader.OnStageCompleted += HandleStageCompleted;
    }

    private void OnDestroy()
    {
        if (_navigator != null)
            _navigator.OnNodeEntered -= OnNodeEntered;
        if (_stageLoader != null)
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
        _mapView.Initialize(mapData, OnNodeButtonClicked, _spriteLoader);
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
    /// 정상 완료 및 게임 오버 시 씬 언로드 후 맵으로 복귀한다.
    /// (게임 오버 시 최종 결과 화면 띄우는 로직은 추후 추가)
    /// </summary>
    private void HandleStageCompleted(StageResult result)
    {
        // 결과 연출 (승리/패배) 판단은 추후 FlowManager 혹은 상위 레벨에서 처리

        _mapView.RefreshNodeStates();
        UnloadAndShowMapAsync().Forget();
    }

    /// <summary>
    /// 디버그용 키 입력 처리.
    /// '1' 키: JournalModel에 적재된 리포트를 JournalUIView로 표시한다.
    /// </summary>
    private void Update()
    {
        if (UnityEngine.InputSystem.Keyboard.current == null)
        {
            return;
        }

        if (UnityEngine.InputSystem.Keyboard.current.digit1Key.wasPressedThisFrame)
        {
            TryShowJournalUI();
        }
    }

    /// <summary>
    /// 1번 키 입력 시 호출.
    /// EventLogAccumulator에 누적된 전체 이벤트 이력을 처음부터 JournalUIView로 표시한다.
    /// 열람 후에도 누적 이력은 지워지지 않는다.
    /// </summary>
    private void TryShowJournalUI()
    {
        if (_journalModel == null)
            return;

        // 이미 일지 팝업이 열려 있는 경우 중복 열기를 방지한다
        if (_journalModel.IsJournalComplete.Value == false)
            return;

        if (_logAccumulator == null || _logAccumulator.HasLogs == false)
        {
            Debug.Log("[MapUIController] 표시할 이벤트 이력이 없습니다.");
            return;
        }

        // ObservableValue equality 방어:
        // 이전과 동일한 텍스트가 CurrentParagraph에 남아 있을 경우 Observer가 발동하지 않으므로
        // 먼저 빈 문자열로 리셋한다. JournalBinder는 빈 문자열에는 반응하지 않으므로 안전하다.
        _journalModel.CurrentParagraph.Value = string.Empty;

        // AllLogs를 JournalModel에 재적재하고 첫 페이지부터 표시한다.
        // SetReports()가 IsJournalComplete를 false로 설정하고,
        // TryNextReport()가 UpdatePageState()를 통해 CurrentParagraph를 갱신한다.
        // JournalBinder._paragraphObserver가 발동해 SetVisible(true)와 ShowText()를 처리한다.
        _journalModel.SetReports(_logAccumulator.AllLogs);
        _journalModel.TryNextReport();
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
            ShowMap(_cachedMapData);
        }
    }

    private async UniTaskVoid UnloadAndShowMapAsync()
    {
        try
        {
            // 이벤트/전투 씬 언로드 (이 시점에 EventJournalProvider.Dispose()가 실행되어
            // 버퍼가 EventLogAccumulator로 플러시된다)
            await _stageLoader.UnloadCurrentStage();

            // 자동 팝업 표시 흐름 제거:
            // CollectDailyLogs() 호출 및 WaitUntil 블로킹을 모두 제거한다.
            // 이력 열람은 1번 키 입력을 통해서만 가능하다.

            ShowMap(_cachedMapData);
        }
        catch (Exception e)
        {
            Debug.LogError($"[MapUIController] 씬 언로드 실패: {e.Message}");
        }
    }
}
