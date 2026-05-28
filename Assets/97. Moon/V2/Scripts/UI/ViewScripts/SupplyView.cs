using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class SupplyView : MonoBehaviour
{
    private VisualElement _root;
    private SupplyManager _supplyManager;

    // 💥 [실시간 가이드 레이블 캐싱 명세]
    private Label _lblInfoTitle;
    private Label _lblInfoDesc;
    private Label _lblInfoCost;

    [Inject]
    public void Construct(SupplyManager supplyManager)
    {
        _supplyManager = supplyManager;
    }

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.style.display = DisplayStyle.None;

        // 1. 신규 정보창 레이블 쿼리 맵핑
        _lblInfoTitle = _root.Q<Label>("supply-info-title");
        _lblInfoDesc = _root.Q<Label>("supply-info-desc");
        _lblInfoCost = _root.Q<Label>("supply-info-cost");

        // 2. 버튼 클릭 기능 연결
        _root.Q<Button>("btn-reinforce").clicked += () => _supplyManager.RequestReinforcement();
        _root.Q<Button>("btn-normal").clicked += () => _supplyManager.RequestSupply(SupplyType.Normal_Supply);
        _root.Q<Button>("btn-special").clicked += () => _supplyManager.RequestSupply(SupplyType.Special_Supply);
        _root.Q<Button>("btn-close-supply").clicked += Close;

        // 3. 💥 [리더님 기획] 마우스 호버(MouseEnter / MouseLeave) 실시간 툴팁 연동 배선작업
        BindHoverEvents("btn-reinforce", "증원 요청", "새로운 탐사대원을 고용합니다.", 300);
        BindHoverEvents("btn-normal", "일반 보급품 요청", "일반 소모품을 요청합니다.", 100);
        BindHoverEvents("btn-special", "특제 보급품 요청", "정신 각성제를 요청합니다.", 500);

        ResetInfoPanel();
    }

    // 📊 실시간 마우스 감지 툴팁 매퍼 헬퍼 함수
    private void BindHoverEvents(string btnName, string title, string description, int cost)
    {
        var btn = _root.Q<Button>(btnName);
        if (btn == null) return;

        // 마우스가 들어왔을 때: 40px 유니티 6 감화 태그 입혀서 텍스트 밀어넣기
        btn.RegisterCallback<MouseEnterEvent>(evt => 
        {
            _lblInfoTitle.text = title;
            _lblInfoDesc.text = $"<line-height=120%>{description}</line-height>";
            _lblInfoCost.text = $"필요 개척 데이터: {cost}";
            _lblInfoCost.style.visibility = Visibility.Visible;
        });

        // 마우스가 나갔을 때: 다시 기본 안내 문구로 롤백
        btn.RegisterCallback<MouseLeaveEvent>(evt => ResetInfoPanel());
    }

    private void ResetInfoPanel()
    {
        if (_lblInfoTitle == null || _lblInfoDesc == null || _lblInfoCost == null) return;

        _lblInfoTitle.text = "보급소 안내";
        _lblInfoDesc.text = "<line-height=120%>원하는 보급 아이콘 위에 마우스를 올리면 상세 기능과 정산 비용 정보가 이곳에 브리핑됩니다.</line-height>";
        _lblInfoCost.style.visibility = Visibility.Hidden; // 평소엔 비용창 숨김
    }

    public void Open() => _root.style.display = DisplayStyle.Flex;
    public void Close() => _root.style.display = DisplayStyle.None;
}