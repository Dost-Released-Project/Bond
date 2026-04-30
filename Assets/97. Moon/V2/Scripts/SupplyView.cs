using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class SupplyView : MonoBehaviour
{
    private VisualElement _root;
    private ISupplyManager _supplyManager;
    private ITotalInventory _totalInventory;

    [Inject]
    public void Construct(SupplyManager supplyManager, ITotalInventory totalInventory)
    {
        _supplyManager = supplyManager;
        _totalInventory = totalInventory;
    }

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.style.display = DisplayStyle.None; // 초기엔 비활성

        // 버튼 연결 (UXML의 Name과 일치해야 함)
        _root.Q<Button>("btn-reinforce").clicked += () => _supplyManager.RequestReinforcement();
        _root.Q<Button>("btn-normal").clicked += () => RequestNormalSupply();
        _root.Q<Button>("btn-special").clicked += () => RequestSpecialSupply();
        _root.Q<Button>("btn-close-supply").clicked += Close;
    }

    public void Open() => _root.style.display = DisplayStyle.Flex;
    public void Close() => _root.style.display = DisplayStyle.None;

    private void RequestNormalSupply()
    {
        // SupplyManager에서 아이템을 직접 받는 대신, 여기서 인벤토리에 추가 명령
        // (SupplyManager가 TotalInventory를 주입받아 처리하도록 설계됨)
        _supplyManager.RequestSupply(SupplyType.Normal_Supply);
    }

    private void RequestSpecialSupply()
    {
        _supplyManager.RequestSupply(SupplyType.Special_Supply);
    }
}
