using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 맵의 개별 노드를 UI Button으로 표현하는 컴포넌트.
/// MapView.DrawNodes()에서 노드마다 하나씩 Instantiate된다.
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

    /// <summary>
    /// 노드 데이터와 시각 정보를 받아 이 뷰를 초기화한다.
    /// </summary>
    /// <param name="node">연결할 맵 노드 데이터</param>
    /// <param name="icon">스테이지 타입 아이콘 스프라이트</param>
    /// <param name="onClickCallback">버튼 클릭 시 호출할 콜백 (인자: 노드 Id)</param>
    public void Setup(MapNode node, Sprite icon, System.Action<int> onClickCallback)
    {
        _node = node;
        _onClickCallback = onClickCallback;

        if (_icon != null && icon != null)
            _icon.sprite = icon;

        // 중복 등록 방지 후 클릭 이벤트 연결
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnButtonClicked);

        RefreshState();
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

    private void OnButtonClicked()
    {
        _onClickCallback?.Invoke(_node.Id);
    }
}
