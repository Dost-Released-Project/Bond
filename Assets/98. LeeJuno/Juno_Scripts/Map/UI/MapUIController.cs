using System;
using System.Collections.Generic;
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
    [SerializeField] private UnityEngine.UI.Button _retreatButton;

    private IMapNavigator _navigator;
    private IStageLoader _stageLoader;
    private ISpriteLoader _spriteLoader;
    private JournalSystem _journalSystem;
    private JournalModel _journalModel;
    private IJournalVisualizer _journalView;
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
        IJournalVisualizer journalView,
        EventLogAccumulator logAccumulator)
    {
        _navigator = navigator;
        _stageLoader = stageLoader;
        _spriteLoader = spriteLoader;
        _journalSystem = journalSystem;
        _journalModel = journalModel;
        _journalView = journalView;
        _logAccumulator = logAccumulator;
        _navigator.OnNodeEntered += OnNodeEntered;
        _stageLoader.OnStageCompleted += HandleStageCompleted;
    }

    private void Start()
    {
        if (_retreatButton != null)
        {
            // 람다식: UnityEvent<> 의 AddListener 는 void 반환 메서드만 직접 전달 가능하므로
            // 파라미터 없는 메서드를 Action 형태로 래핑하기 위해 사용
            _retreatButton.onClick.AddListener(() => OnRetreatButtonClicked());
        }
        else
        {
            Debug.LogWarning("[MapUIController] _retreatButton 이 연결되지 않았습니다.");
        }
    }

    private void OnDestroy()
    {
        if (_navigator != null)
            _navigator.OnNodeEntered -= OnNodeEntered;
        if (_stageLoader != null)
            _stageLoader.OnStageCompleted -= HandleStageCompleted;
        if (_retreatButton != null)
            _retreatButton.onClick.RemoveAllListeners();
    }

    /// <summary>
    /// 맵 패널을 열고 지정한 맵 데이터를 렌더링한다.
    /// 새 챕터 시작 또는 스테이지 완료 후 복귀 시 호출한다.
    /// </summary>
    public void ShowMap(MapData mapData)
    {
        _cachedMapData = mapData;
        _mapPanel.SetActive(true);
        if (_retreatButton != null)
            _retreatButton.gameObject.SetActive(true);
        _mapView.Initialize(mapData, OnNodeButtonClicked, _spriteLoader);
    }

    /// <summary>
    /// 맵 패널을 닫는다. 노드 선택 후 스테이지 씬 로드 직전 호출된다.
    /// 퇴각 버튼도 함께 숨긴다.
    /// </summary>
    public void HideMap()
    {
        _mapPanel.SetActive(false);
        if (_retreatButton != null)
            _retreatButton.gameObject.SetActive(false);
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

    /// <summary>
    /// 퇴각 버튼 클릭 시 호출된다.
    /// 확인 다이얼로그를 표시하고 확인 선택 시에만 퇴각을 진행한다.
    /// </summary>
    private void OnRetreatButtonClicked()
    {
        // 람다식: 비동기 예외를 void 컨텍스트에서 명시적으로 기록하기 위해 사용한다
        RetreatWithConfirmAsync().Forget(e => Debug.LogError(e));
    }

    /// <summary>
    /// 퇴각 확인 다이얼로그를 표시하고 확인 선택 시 퇴각을 진행한다.
    /// </summary>
    private async UniTask RetreatWithConfirmAsync()
    {
        bool confirmed = await ShowRetreatConfirmAsync();
        if (confirmed == false)
            return;

        RetreatToTownAsync().Forget();
    }

    /// <summary>
    /// JournalUIView 를 사용해 "퇴각 하시겠습니까?" 확인 다이얼로그를 표시한다.
    /// JournalBinder 가 설정한 OnOptionSelected 콜백을 저장하고 완료 후 복원한다.
    /// IJournalVisualizer 가 없으면 즉시 true 를 반환한다.
    /// </summary>
    private async UniTask<bool> ShowRetreatConfirmAsync()
    {
        if (_journalView == null)
            return true;

        // JournalBinder 가 설정한 기존 콜백을 저장한다 — 완료 후 복원해 일지 기능이 정상 동작하게 한다
        Action<JournalOption> savedCallback = _journalView.OnOptionSelected;

        UniTaskCompletionSource<bool> tcs = new UniTaskCompletionSource<bool>();

        _journalView.ClearUI();
        _journalView.ShowText("퇴각 하시겠습니까?", isTyping: false);

        List<JournalOption> options = new List<JournalOption>
        {
            new JournalOption { text = "확인", actionKey = string.Empty, isEnabled = true },
            new JournalOption { text = "취소", actionKey = string.Empty, isEnabled = true },
        };

        // 람다식: 확인/취소 선택 결과를 UniTaskCompletionSource 에 전달하기 위해 사용한다
        _journalView.OnOptionSelected = option =>
        {
            bool isConfirm = option.text == "확인";
            tcs.TrySetResult(isConfirm);
        };

        _journalView.SetOptions(options);
        _journalView.SetVisible(true);

        bool result = await tcs.Task;

        _journalView.SetVisible(false);
        _journalView.OnOptionSelected = savedCallback;

        return result;
    }

    private async UniTaskVoid RetreatToTownAsync()
    {
        // 스테이지 씬이 로드 중이거나 로드된 상태면 먼저 언로드한다
        if (_stageLoader.IsLoading == false)
        {
            try
            {
                await _stageLoader.UnloadCurrentStage();
            }
            catch (Exception e)
            {
                Debug.LogError($"[MapUIController] 퇴각 중 씬 언로드 실패: {e.Message}");
                // 언로드 실패 시에도 마을 복귀를 시도한다
            }
        }

        SceneLoader.Load("Town");
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
