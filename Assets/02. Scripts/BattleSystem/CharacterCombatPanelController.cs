using System;

public class CharacterCombatPanelController : IDisposable
{
    private BaseCharacter _character;
    private bool _isMyTurn;
    private int _selectedSlotIndex = -1;

    public event Action<BaseCharacter> OnCharacterUpdated;
    public event Action<bool> OnTurnStateChanged;
    public event Action<int> OnSkillSelected;  // -1이면 선택 해제

    public void SetCharacter(BaseCharacter character)
    {
        if (_character != null)
        {
            _character.onPlayerTurnStarted -= HandlePlayerTurnStarted;
            _character.onPlayerTurnEnded -= HandlePlayerTurnEnded;
        }

        _character = character;
        _character.onPlayerTurnStarted += HandlePlayerTurnStarted;
        _character.onPlayerTurnEnded += HandlePlayerTurnEnded;
        OnCharacterUpdated?.Invoke(_character);
    }

    public void SetMyTurn(bool isMyTurn)
    {
        _isMyTurn = isMyTurn;
        if (!isMyTurn) _selectedSlotIndex = -1;
        OnTurnStateChanged?.Invoke(_isMyTurn);
    }

    public void SelectSkill(int slotIndex)
    {
        if (!_isMyTurn) return;
        if (slotIndex < 0 || slotIndex >= 4) return;

        // 이미 선택된 스킬을 다시 누르면 취소
        if (_selectedSlotIndex == slotIndex)
        {
            _selectedSlotIndex = -1;
            OnSkillSelected?.Invoke(-1);
            _character?.ConfirmSkillSelection(null); // 스킬 선택 취소
            return;
        }

        _selectedSlotIndex = slotIndex;
        OnSkillSelected?.Invoke(slotIndex);

        if (_character?.Skills[slotIndex] != null)
        {
            // 스킬을 확정하지 않고 '선택 중' 상태로 넘김
            _character.ConfirmSkillSelection(_character.Skills[slotIndex]);
            // 여기서 SetMyTurn(false)를 호출하지 않아야 유저가 다른 스킬로 바꿀 수 있음
        }
    }

    private void HandlePlayerTurnStarted(BaseCharacter character)
    {
        SetMyTurn(true);
    }

    private void HandlePlayerTurnEnded(BaseCharacter character)
    {
        SetMyTurn(false);
    }

    /// <summary>탐사 가방 인벤토리 슬롯에서 장신구 슬롯 방향으로 드래그 장착/스왑을 수행합니다.</summary>
    public void EquipAccessoryFromDrag(IInventory sourceInventory, int invIndex, int charSlotIndex, CharacterItemService itemService)
    {
        if (_character == null) return;
    
        // CharacterDetailController가 사용하던 규칙과 완전히 동일하게 
        // CharacterItemService를 찔러 인벤토리 풀 상태 스왑 장착 연산을 대행시킵니다.
        itemService.EquipFromDrag(sourceInventory, invIndex, charSlotIndex);
    }

    public void UnequipAccessory(int accSlotIndex, ExpeditionInventory supplies, CharacterItemService itemService)
    {
        // AccessoryItem은 인벤토리에서 관리되므로 목적지 IInventory를 Presenter에서 전달받는다
        if (_character == null) return;
        itemService.UnequipToInventory(_character, accSlotIndex, supplies);
    }

    /// <summary>패널 파괴 시 소유자(프레젠터)가 호출. 마지막으로 구독 중이던 캐릭터의 턴 이벤트를 끊는다.
    /// 이걸 안 하면 장수명 BaseCharacter → 이 컨트롤러 → (파괴된) 프레젠터 로 이어지는 참조 사슬을
    /// 캐릭터가 살아있는 한 GC가 수거하지 못해 누수 + 좀비 콜백이 발생한다.</summary>
    public void Dispose()
    {
        if (_character != null)
        {
            _character.onPlayerTurnStarted -= HandlePlayerTurnStarted;
            _character.onPlayerTurnEnded   -= HandlePlayerTurnEnded;
            _character = null;
        }
    }
}
