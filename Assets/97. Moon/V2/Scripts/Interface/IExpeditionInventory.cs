using UnityEngine;

// 탐사 인벤토리: 슬롯 제한 및 장비 탈착
public interface IExpeditionInventory : IInventory
{
    bool EquipItem(BaseCharacter character, string itemID);
    void UnequipItem(BaseCharacter character, Equipment slot);
}