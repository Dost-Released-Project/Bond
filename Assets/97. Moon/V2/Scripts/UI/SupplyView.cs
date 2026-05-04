using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class SupplyView : MonoBehaviour
{
    private VisualElement _root;
    private SupplyManager _supplyManager;

    [Inject]
    public void Construct(SupplyManager supplyManager)
    {
        // ITotalInventory는 Manager에서 처리하므로 View에서는 Manager만 주입받으면 됨
        _supplyManager = supplyManager;
    }

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.style.display = DisplayStyle.None;

        // 버튼 연결 및 람다식을 통한 직접 호출 (불필요한 내부 함수 제거)
        _root.Q<Button>("btn-reinforce").clicked += () => _supplyManager.RequestReinforcement();
        _root.Q<Button>("btn-normal").clicked += () => _supplyManager.RequestSupply(SupplyType.Normal_Supply);
        _root.Q<Button>("btn-special").clicked += () => _supplyManager.RequestSupply(SupplyType.Special_Supply);
        _root.Q<Button>("btn-close-supply").clicked += Close;
    }

    public void Open() => _root.style.display = DisplayStyle.Flex;
    public void Close() => _root.style.display = DisplayStyle.None;
}