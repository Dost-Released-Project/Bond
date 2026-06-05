using System.Threading;
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
    [Range(0, 1)] [SerializeField] private float _fadeLerpAmount = 0.75f;

    private static readonly Color _mapBackgroundColor = new Color(142f / 255f, 130f / 255f, 108f / 255f, 1f);


    private MapNode _node;
    private System.Action<int> _onClickCallback;
    private AsyncOperationHandle<Sprite> _iconHandle; // 로드 핸들 — OnDestroy에서 Release
    private bool _iconHandleValid; // 핸들 유효 여부 플래그
    private ISpriteLoader _spriteLoader;
    private CancellationTokenSource _loadCts; // 비동기 로드 취소용 — Setup 재호출 시 이전 작업 취소
    private Color _originalBackgroundColor = Color.white;

    /// <summary>
    /// 노드 데이터와 시각 정보(주소 기반)를 받아 이 뷰를 초기화한다.
    /// 아이콘 로드는 비동기로 진행된다.
    /// </summary>
    /// <param name="node">연결할 맵 노드 데이터</param>
    /// <param name="iconAddress">Addressables 아이콘 주소. 비어 있으면 아이콘 비표시.</param>
    /// <param name="fallbackIcon">iconAddress 로드 실패 또는 빈 경우 사용할 스프라이트</param>
    /// <param name="onClickCallback">버튼 클릭 시 호출할 콜백 (인자: 노드 Id)</param>
    /// <param name="spriteLoader">Addressables Sprite 비동기 로드 서비스. 호출자(MapView)가 DI로 전달한다.</param>
    public void Setup(MapNode node, string iconAddress, Sprite fallbackIcon, System.Action<int> onClickCallback,
        ISpriteLoader spriteLoader)
    {
        _spriteLoader = spriteLoader;

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

        // Button 자동 틴팅을 무력화한다 — 색상 제어를 RefreshState에서 직접 처리한다
        ColorBlock colorBlock = _button.colors;
        colorBlock.disabledColor = Color.white;
        _button.colors = colorBlock;

        // 중복 등록 방지 후 클릭 이벤트 연결
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnButtonClicked);

        RefreshState();

        // 주소가 있으면 비동기 로드 시작 — 이전 로드 작업을 취소한 뒤 새 토큰으로 시작한다
        if (string.IsNullOrEmpty(iconAddress) == false)
        {
            _loadCts?.Cancel();
            _loadCts?.Dispose();
            _loadCts = new CancellationTokenSource();
            // 람다: UniTaskVoid 반환 메서드에 토큰 전달 후 Forget — 예외는 메서드 내부에서 처리
            LoadIconAsync(iconAddress, _loadCts.Token).Forget();
        }
    }

    /// <summary>
    /// 노드의 현재 State에 맞게 버튼 활성화 여부와 강조 표시를 갱신한다.
    /// MapView.RefreshNodeStates()에서 일괄 호출된다.
    /// </summary>
    public void RefreshState()
    {
        if (_node == null)
            return;

        bool isAvailable = _node.State == NodeState.Available;
        _button.interactable = isAvailable;

        if (isAvailable)
        {
            if (_background != null)
                _background.color = _originalBackgroundColor;
            if (_icon != null)
                _icon.color = Color.white;
        }
        else
        {
            if (_background != null)
                _background.color = Color.Lerp(_originalBackgroundColor, _mapBackgroundColor, _fadeLerpAmount);
            if (_icon != null)
                _icon.color = Color.Lerp(Color.white, _mapBackgroundColor, _fadeLerpAmount);
        }

        if (_availableIndicator != null)
            _availableIndicator.SetActive(isAvailable);
    }

    /// <summary>
    /// 배경 이미지의 색상을 설정한다. 스테이지 타입별 색상 구분에 사용된다.
    /// </summary>
    public void SetColor(Color color)
    {
        _originalBackgroundColor = color;
        RefreshState();
    }

    private void OnDestroy()
    {
        // 진행 중인 로드 작업 취소 및 CancellationTokenSource 해제
        _loadCts?.Cancel();
        _loadCts?.Dispose();

        // 로드된 핸들이 있으면 메모리 해제 — IsValid() 체크로 이미 무효화된 핸들의 크래시 방지
        if (_iconHandleValid && _iconHandle.IsValid())
            Addressables.Release(_iconHandle);
    }

    /// <summary>
    /// iconAddress로 Sprite를 비동기 로드해 _icon에 적용한다.
    /// 로드 실패 시 fallback(Setup 시 적용된 스프라이트)을 유지한다.
    /// UniTask.Forget 방식으로 호출 — 예외를 throw하지 않으므로 별도 에러 핸들러 불필요.
    /// </summary>
    private async UniTaskVoid LoadIconAsync(string iconAddress, CancellationToken token)
    {
        // ISpriteLoader 에게 로드를 위임. 핸들 소유권은 이 클래스가 유지한다.
        _iconHandle = await _spriteLoader.LoadAsync(iconAddress);
        _iconHandleValid = true;

        // 취소된 경우 핸들을 즉시 해제하고 종료 — Setup 재호출 시 이전 작업 취소 처리
        if (token.IsCancellationRequested)
        {
            if (_iconHandle.IsValid())
            {
                Addressables.Release(_iconHandle);
                _iconHandleValid = false;
            }

            return;
        }

        // 로드 완료 시점에 오브젝트가 이미 파괴되었을 수 있으므로 null 체크
        if (this == null)
            return;

        // try-catch 의존 구조 제거 — Status 체크로 성공/실패를 명시적으로 판단한다
        if (_iconHandle.Status != AsyncOperationStatus.Succeeded)
        {
            Debug.LogWarning($"[MapNodeView] 아이콘 로드 실패 (address={iconAddress})");
            if (_iconHandle.IsValid())
            {
                Addressables.Release(_iconHandle);
                _iconHandleValid = false;
            }

            return;
        }

        if (_icon != null)
            _icon.sprite = _iconHandle.Result;
    }

    private void OnButtonClicked()
    {
        _onClickCallback?.Invoke(_node.Id);
    }
}