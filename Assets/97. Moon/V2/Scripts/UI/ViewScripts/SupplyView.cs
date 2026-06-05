using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class SupplyView : MonoBehaviour
{
    private VisualElement _root;
    private SupplyManager _supplyManager;

    // [실시간 가이드 레이블 캐싱 명세]
    private Label _lblInfoTitle;
    private Label _lblInfoDesc;
    private Label _lblInfoCost;

    // 🔗 현재 활성화된 보급소 건물 오브젝트 및 데이터 추적용 캐시
    private BuildingObject _targetBuildingObject;
    private BuildingLevelData _targetLevelData;

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

        // 2. 버튼 클릭 기능 연결 (이용 제한 검증 및 카운트 차감 레이어 래핑)
        _root.Q<Button>("btn-reinforce").clicked += () => ExecuteSupplyAction(() => _supplyManager.RequestReinforcement());
        _root.Q<Button>("btn-normal").clicked += () => ExecuteSupplyAction(() => _supplyManager.RequestSupply(SupplyType.Normal_Supply));
        _root.Q<Button>("btn-special").clicked += () => ExecuteSupplyAction(() => _supplyManager.RequestSupply(SupplyType.Special_Supply));
        _root.Q<Button>("btn-close-supply").clicked += Close;

        // 3. 마우스 호버(MouseEnter / MouseLeave) 실시간 툴팁 연동 배선작업
        BindHoverEvents("btn-reinforce", "증원 요청", "새로운 탐사대원을 고용합니다.", 300);
        BindHoverEvents("btn-normal", "일반 보급품 요청", "일반 소모품을 요청합니다.", 100);
        BindHoverEvents("btn-special", "특제 보급품 요청", "정신 각성제를 요청합니다.", 500);

        ResetInfoPanel();
    }

    // 💥 [수정] SettlementManager 등에서 이 UI를 열 때 타겟 건물 오브젝트를 수용하도록 포트 확장
    public void Open(BuildingObject building)
    {
        _targetBuildingObject = building;
        if (_targetBuildingObject != null && _targetBuildingObject.Data != null)
        {
            _targetLevelData = _targetBuildingObject.Data.GetLevelData(_targetBuildingObject.CurrentLevel);
        }

        _root.style.display = DisplayStyle.Flex;
        ResetInfoPanel();
    }

    public void Close() => _root.style.display = DisplayStyle.None;

    // 💥 [신규] 보급 기능 처리 및 이용 카운터 차감 공통 래퍼 함수
    private void ExecuteSupplyAction(System.Action supplyAction)
    {
        if (_targetBuildingObject == null || _targetLevelData.level == 0) return;

        // 1. 이용 한도에 도달했는지 선 검증
        if (_targetBuildingObject.Counter != null && _targetBuildingObject.Counter.IsUseLimitReached())
        {
            Debug.LogWarning($"[이용 불가] 보급소의 이용 한도를 초과했습니다!");
            return;
        }

        // 2. 실제 보급 비즈니스 기능(SupplyManager) 수행
        supplyAction?.Invoke();

        // 3. 카운터 증가 및 월드 툴팁 강제 리프레시 동기화
        if (_targetBuildingObject.Counter != null)
        {
            _targetBuildingObject.Counter.UseBuilding();

            // UI 설명 창 실시간 정보 갱신
            ResetInfoPanel();

            if (_targetBuildingObject.Visuals != null)
            {
                _targetBuildingObject.Visuals.ForceRefreshTooltip();
            }
        }
    }

    // 📊 실시간 마우스 감지 툴팁 매퍼 헬퍼 함수
    private void BindHoverEvents(string btnName, string title, string description, int cost)
    {
        var btn = _root.Q<Button>(btnName);
        if (btn == null) return;

        btn.RegisterCallback<MouseEnterEvent>(evt => 
        {
            int maxUses = _targetLevelData.level != 0 ? _targetLevelData.maxUses : 0;
            int curUses = (_targetBuildingObject != null && _targetBuildingObject.Counter != null) ? _targetBuildingObject.Counter.CurrentTurnUses : 0;
            int remainUses = Mathf.Max(maxUses - curUses, 0);

            _lblInfoTitle.text = title;
            _lblInfoDesc.text = $"<line-height=120%>{description}\n\n<b>[보급소 이용 제한]</b>\n남은 횟수: {remainUses} / {maxUses}회</line-height>";
            _lblInfoCost.text = remainUses <= 0 ? "<color=red>이용 제한 초과 (보급 불가)</color>" : $"필요 개척 데이터: {cost}";
            _lblInfoCost.style.visibility = Visibility.Visible;
        });

        btn.RegisterCallback<MouseLeaveEvent>(evt => ResetInfoPanel());
    }

    private void ResetInfoPanel()
    {
        if (_lblInfoTitle == null || _lblInfoDesc == null || _lblInfoCost == null) return;

        int maxUses = _targetLevelData.level != 0 ? _targetLevelData.maxUses : 0;
        int curUses = (_targetBuildingObject != null && _targetBuildingObject.Counter != null) ? _targetBuildingObject.Counter.CurrentTurnUses : 0;
        int remainUses = Mathf.Max(maxUses - curUses, 0);

        _lblInfoTitle.text = "보급소 안내";
        _lblInfoDesc.text = $"<line-height=120%>원하는 보급 아이콘 위에 마우스를 올리면 상세 기능과 정산 비용 정보가 이곳에 브리핑됩니다.\n\n<b>[현재 보급소 상태]</b>\n남은 횟수: {remainUses} / {maxUses}회</line-height>";
        _lblInfoCost.style.visibility = Visibility.Hidden; 
    }
}