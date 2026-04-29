using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;

/// <summary>
/// 맵의 개별 노드를 UI Button으로 표현하는 컴포넌트.
/// MapView.DrawNodes()에서 노드마다 하나씩 Instantiate된다.
/// 아이콘은 Addressables 주소를 받아 자체적으로 비동기 로드한다.
///
/// Inspector 연결 필요:
///   _button            — 클릭을 받는 UI Button
///   _icon              — 스테이지 타입 아이콘 Image
///   _background        — 노드 배경 Image (스테이지 타입 색상 적용)
///   _availableIndicator— 선택 가능 상태일 때 표시할 강조 오브젝트
/// </summary>
public class MapNodeView : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private Image _icon;
    [SerializeField] private Image _background;
    [SerializeField] private GameObject _availableIndicator;

    private MapNode _node;
    private System.Action<int> _onClickCallback;
    private AsyncOperationHandle<Sprite> _iconHandle; // 로드 핸들 — OnDestroy에서 Release
    private bool _iconHandleValid;                    // 핸들 유효 여부 플래그

    /// <summary>
    /// 노드 데이터와 시각 정보(주소 기반)를 받아 이 뷰를 초기화한다.
    /// 아이콘 로드는 비동기로 진행된다.
    /// </summary>
    /// <param name="node">연결할 맵 노드 데이터</param>
    /// <param name="iconAddress">Addressables 아이콘 주소. 비어 있으면 아이콘 비표시.</param>
    /// <param name="fallbackIcon">iconAddress 로드 실패 또는 빈 경우 사용할 스프라이트</param>
    /// <param name="onClickCallback">버튼 클릭 시 호출할 콜백 (인자: 노드 Id)</param>
    public void Setup(MapNode node, string iconAddress, Sprite fallbackIcon, System.Action<int> onClickCallback)
    {
        // _button 미연결 시 즉시 종료
        if (_button == null)
        {
            Debug.LogError("[MapNodeView] _button이 연결되지 않았습니다.", this);
            return;
        }

        // 이전 핸들 정리 — Setup 재호출 시 이전 비동기 핸들 누수 방지
        if (_iconHandleValid && _iconHandle.IsValid())
        {
            Addressables.Release(_iconHandle);
            _iconHandleValid = false;
        }

        _node = node;
        _onClickCallback = onClickCallback;

        // fallback 먼저 적용 — 로드 완료 전까지 표시할 아이콘
        if (_icon != null && fallbackIcon != null)
            _icon.sprite = fallbackIcon;

        // 중복 등록 방지 후 클릭 이벤트 연결
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnButtonClicked);

        RefreshState();

        // 주소가 있으면 비동기 로드 시작
        if (string.IsNullOrEmpty(iconAddress) == false)
            LoadIconAsync(iconAddress).Forget(); // 반환값 불필요 — 실패 시 catch 내부에서 LogWarning 처리
    }

    /// <summary>
    /// 노드의 현재 State에 맞게 버튼 활성화 여부와 강조 표시를 갱신한다.
    /// MapView.RefreshNodeStates()에서 일괄 호출된다.
    /// </summary>
    public void RefreshState()
    {
        if (_node == null)
            return;

        bool isInteractable = _node.State == NodeState.Available;
        _button.interactable = isInteractable;

        // 선택 가능한 노드에만 강조 인디케이터 표시
        if (_availableIndicator != null)
            _availableIndicator.SetActive(isInteractable);
    }

    /// <summary>
    /// 배경 이미지의 색상을 설정한다. 스테이지 타입별 색상 구분에 사용된다.
    /// </summary>
    public void SetColor(Color color)
    {
        if (_background != null)
            _background.color = color;
    }

    private void OnDestroy()
    {
        // 로드된 핸들이 있으면 메모리 해제 — IsValid() 체크로 이미 무효화된 핸들의 크래시 방지
        if (_iconHandleValid && _iconHandle.IsValid())
            Addressables.Release(_iconHandle);
    }

    /// <summary>
    /// iconAddress로 Sprite를 비동기 로드해 _icon에 적용한다.
    /// 로드 실패 시 fallback(Setup 시 적용된 스프라이트)을 유지한다.
    /// UniTask.Forget 방식으로 호출 — 람다 없이 메서드 분리로 명확성 확보.
    /// </summary>
    private async UniTaskVoid LoadIconAsync(string iconAddress)
    {
        _iconHandle = Addressables.LoadAssetAsync<Sprite>(iconAddress);
        _iconHandleValid = true;

        try
        {
            Sprite loaded = await _iconHandle.ToUniTask();

            // 로드 완료 시점에 오브젝트가 이미 파괴되었을 수 있으므로 null 체크
            if (this == null)
                return;

            if (_icon != null && loaded != null)
                _icon.sprite = loaded;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning($"[MapNodeView] 아이콘 로드 실패 (address={iconAddress}): {e.Message}");

            // 실패 시 핸들 해제 — OnDestroy에서 이중 해제 방지
            if (_iconHandleValid)
            {
                Addressables.Release(_iconHandle);
                _iconHandleValid = false;
            }
        }
    }

    private void OnButtonClicked()
    {
        _onClickCallback?.Invoke(_node.Id);
    }
}
