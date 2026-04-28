using UnityEngine;
using UnityEngine.UIElements;
using VContainer;
using VContainer.Unity;

public class EquipmentSlotUI : MonoBehaviour
{
    [SerializeField] private int slotIndex;
    private VisualElement _slotVisual;
    private CharacterEquipService _equipService;
    private ITotalInventory _totalInventory;

    [Inject] public void Construct(CharacterEquipService es, ITotalInventory total) { _equipService = es; _totalInventory = total; }

    private void Start()
    {
        _slotVisual = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>($"char-acc-slot-{slotIndex}");
        _slotVisual?.RegisterCallback<PointerUpEvent>(evt => {
            if (InventoryView.CurrentSourceInventory != null)
            {
                _equipService.EquipFromDrag(InventoryView.CurrentSourceInventory, InventoryView.CurrentDraggingIndex, slotIndex);
                InventoryView.ResetDraggingState();
                RefreshUI();
            }
        });
        _slotVisual?.RegisterCallback<PointerDownEvent>(evt => {
            if (evt.button == 1) { 
                // 1. 해제 실행
                _equipService.UnequipToInventory(AdminTestTool.testHero, slotIndex, _totalInventory); 
        
                // 2. [핵심] 즉시 UI 갱신
                RefreshUI(); 
            }
        });
        _totalInventory.OnChanged += RefreshUI;
        RefreshUI();
    }

    public void RefreshUI()
    {
        if (_slotVisual == null) return;
        _slotVisual.Clear();
        var eq = AdminTestTool.testHero?.equip[slotIndex];
        if (eq?.originItem != null)
        {
            var icon = new VisualElement();
            icon.style.backgroundImage = new StyleBackground(eq.originItem.icon);
            icon.style.width = Length.Percent(100); icon.style.height = Length.Percent(100);
            _slotVisual.Add(icon);
        }
    }
}