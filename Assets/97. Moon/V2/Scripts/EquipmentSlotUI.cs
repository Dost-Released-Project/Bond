using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    private VisualElement _root;
    private VisualElement _slotVisual;
    private CharacterEquipService _equipService;

    [Inject]
    public void Construct(CharacterEquipService es) => _equipService = es;

    private void Start()
    {
        var uiDoc = GetComponent<UIDocument>();
    
        // 1. UIDocument 컴포넌트 자체가 있는지 확인
        if (uiDoc == null) {
            Debug.LogError($"{gameObject.name}에 UIDocument가 없습니다!");
            return;
        }

        // 2. rootVisualElement가 유효한지 확인
        var root = uiDoc.rootVisualElement;
        if (root == null) {
            Debug.LogError($"{gameObject.name}의 rootVisualElement가 null입니다. UXML이 할당되었는지 확인하세요.");
            return;
        }

        // 3. 이제 안전하게 찾기
        _root = root;
        _slotVisual = _root.Q<VisualElement>($"char-acc-slot-{slotIndex}");
    
        if (_slotVisual == null) {
            Debug.LogWarning($"UXML에서 'char-acc-slot-{slotIndex}'라는 이름을 가진 요소를 찾을 수 없습니다.");
            return;
        }
        

        // 서비스의 이벤트를 구독하여 누구든 장착을 변경하면 즉시 내 UI 갱신
        _equipService.OnEquipmentChanged += RefreshUI;

        _slotVisual?.RegisterCallback<PointerUpEvent>(evt => {
            if (InventoryView.CurrentSourceInventory != null) {
                _equipService.EquipFromDrag(InventoryView.CurrentSourceInventory, InventoryView.CurrentDraggingIndex, slotIndex);
                InventoryView.ResetDraggingState();
            }
        });

        _slotVisual?.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.button == 1) _equipService.UnequipToInventory(AdminTestTool.testHero, slotIndex);
        });

        RefreshUI();
    }

    public void ToggleWindow() => _root.style.display = (_root.style.display == DisplayStyle.Flex) ? DisplayStyle.None : DisplayStyle.Flex;

    public void RefreshUI()
    {
        if (_slotVisual == null) return;
        _slotVisual.Clear();
        var eq = AdminTestTool.testHero?.Data.Equips[slotIndex];
        if (eq?.originItem != null) {
            var icon = new VisualElement();
            icon.style.backgroundImage = new StyleBackground(eq.originItem.icon);
            icon.style.width = icon.style.height = Length.Percent(100);
            icon.pickingMode = PickingMode.Ignore;
            _slotVisual.Add(icon);
        }
    }
}