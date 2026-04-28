using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class ResourceView : MonoBehaviour
{
    private Label _frontierLabel;
    private Label _woodLabel;
    private Label _oreLabel;
    //private Label _ResourceA;
    private ResourceManager _resourceManager;

    [Inject]
    public void Construct(ResourceManager rm)
    {
        _resourceManager = rm;
        _resourceManager.OnResourcesChanged += UpdateUI;
    }

    private void Start()
    {   
        var root = GetComponent<UIDocument>().rootVisualElement;
        
        // UXML의 ID와 매칭 (label-frontier, label-wood, label-ore)
        _frontierLabel = root.Q<Label>("label-frontier");
        _woodLabel = root.Q<Label>("label-wood");
        _oreLabel = root.Q<Label>("label-ore");
        //_ResourceA = root.Q<Label>("label-resourceA");

        UpdateUI(_resourceManager.FrontierData, _resourceManager.Wood, _resourceManager.Ore);
    }

    private void UpdateUI(int frontier, int wood, int ore)
    {
        if (_frontierLabel != null) _frontierLabel.text = frontier.ToString();
        if (_woodLabel != null) _woodLabel.text = wood.ToString();
        if (_oreLabel != null) _oreLabel.text = ore.ToString();
        //if (_ResourceA != null) _ResourceA.text = (ResourceA).ToString();
    }
}