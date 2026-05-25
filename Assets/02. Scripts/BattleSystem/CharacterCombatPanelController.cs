using System;

public class CharacterCombatPanelController
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
            _character.onPlayerTurnStarted -= HandlePlayerTurnStarted;

        _character = character;
        _character.onPlayerTurnStarted += HandlePlayerTurnStarted;
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

        if (_selectedSlotIndex == slotIndex)
        {
            _selectedSlotIndex = -1;
            OnSkillSelected?.Invoke(-1);
            return;
        }

        _selectedSlotIndex = slotIndex;
        OnSkillSelected?.Invoke(slotIndex);

        if (_character?.Skills[slotIndex] != null)
        {
            _character.ConfirmSkillSelection(_character.Skills[slotIndex]);
            SetMyTurn(false);
        }
    }

    private void HandlePlayerTurnStarted(BaseCharacter character)
    {
        SetMyTurn(true);
    }
}
