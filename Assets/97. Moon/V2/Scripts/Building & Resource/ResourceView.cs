using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class ResourceView : MonoBehaviour
{
    private Label _frontierLabel;
    private Label _woodLabel;
    private Label _oreLabel;
    private ResourceManager _resourceManager;

    [Inject]
    public void Construct(ResourceManager rm)
    {
        _resourceManager = rm;
        // 수정: 변경된 이벤트 구독 (형식: Action<ResourceType, ResourceData>)
        _resourceManager.OnResourceChanged += (type, data) => UpdateUI();
    }

    private void Start()
    {   
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        _frontierLabel = root.Q<Label>("label-frontier");
        _woodLabel = root.Q<Label>("label-wood");
        _oreLabel = root.Q<Label>("label-ore");

        UpdateUI(); // 초기화
    }

    // 매개변수 없는 방식으로 통합하여 어디서든 호출 가능하게 변경
    private void UpdateUI()
    {
        if (_frontierLabel != null) _frontierLabel.text = _resourceManager.FrontierData.ToString();
        if (_woodLabel != null) _woodLabel.text = _resourceManager.Wood.ToString();
        if (_oreLabel != null) _oreLabel.text = _resourceManager.Ore.ToString();
        
        // 나중에 새로운 자원 UI가 추가된다면 여기에 한 줄만 추가하면 됩니다.
    }
}