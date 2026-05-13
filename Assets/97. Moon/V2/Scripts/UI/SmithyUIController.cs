using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class SmithyUIController : MonoBehaviour
{
    private VisualElement _root;
    private Label _itemNameLabel, _statInfoLabel, _costLabel;
    private Button _upgradeButton;
    
    private Equipment _selectedEquipment;
    private BaseCharacter _character;
    private int _currentSmithyLevel; // 현재 건물 레벨 저장

    [Inject] private BuildingService _buildingService;

    private void Awake()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.style.display = DisplayStyle.None; // 초기 비활성화

        _itemNameLabel = _root.Q<Label>("item-name");
        _statInfoLabel = _root.Q<Label>("stat-info");
        _costLabel = _root.Q<Label>("upgrade-cost");
        _upgradeButton = _root.Q<Button>("btn-upgrade");

        _root.Q<Button>("slot-weapon").clicked += () => SelectEquipment(_character?.Data.Weapon);
        _root.Q<Button>("slot-armor").clicked += () => SelectEquipment(_character?.Data.Armor);
        _root.Q<Button>("btn-close").clicked += Close;
        
        _upgradeButton.clicked += OnUpgradeClicked;
    }

    public void Open(BaseCharacter hero, int smithyLevel)
    {
        _character = hero;
        _currentSmithyLevel = smithyLevel;
        _root.style.display = DisplayStyle.Flex;
        
        // 기본적으로 무기 선택 상태로 시작
        SelectEquipment(_character.Data.Weapon);
    }

    public void Close() => _root.style.display = DisplayStyle.None;

    private void SelectEquipment(Equipment eq)
    {
        _selectedEquipment = eq;
        RefreshDetail();
    }

    private void RefreshDetail()
    {
        if (_selectedEquipment == null) return;

        _itemNameLabel.text = $"{_selectedEquipment.itemName} (+{_selectedEquipment.upgradeLevel})";
        _statInfoLabel.text = $"STR: {_selectedEquipment.bonusSTR} \nAGI: {_selectedEquipment.bonusAGI} \nINT: {_selectedEquipment.bonusINT}";
        
        // 비용 계산 (광석 추가)
        int costLog = (_selectedEquipment.upgradeLevel + 1) * 100;
        int costOre = (_selectedEquipment.upgradeLevel + 1) * 10;
        _costLabel.text = $"소모: 개척데이터 {costLog} / 광석 {costOre}";
        
        // 버튼 활성화 조건: 최대 강화치 미만 && 현재 대장간 레벨이 강화 레벨보다 높음
        bool canUpgrade = _selectedEquipment.upgradeLevel < Equipment.MAX_UPGRADE && 
                          _selectedEquipment.upgradeLevel < _currentSmithyLevel;
                          
        _upgradeButton.SetEnabled(canUpgrade);

        if (_selectedEquipment.upgradeLevel >= _currentSmithyLevel)
            _costLabel.text += "\n<color=red>(대장간 레벨 부족)</color>";
    }

    private void OnUpgradeClicked()
    {
        if (_selectedEquipment == null) return;
        
        // 실제 강화 로직 수행 (smithyLevel 전달)
        _buildingService.UpgradeEquipment(_character, _selectedEquipment, _currentSmithyLevel);
        RefreshDetail();
    }
}