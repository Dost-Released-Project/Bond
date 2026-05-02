using UnityEngine;
using VContainer;
using System;

public class CharacterItemService
{
    private ITotalInventory _totalInventory;
    private IExpeditionInventory _expeditionInventory;
    private InventoryUIService _uiService; // UI 상태 참조를 위해 추가

    public event Action OnEquipmentChanged;

    [Inject]
    public void Construct(ITotalInventory total, IExpeditionInventory exp, InventoryUIService uiService)
    {
        _totalInventory = total;
        _expeditionInventory = exp;
        _uiService = uiService;
    }

    // [이관된 기능] 아이템 사용 로직
    public void UseItem(BaseCharacter hero, IInventory sourceInv, int index)
    {
        var slot = sourceInv.GetSlot(index);
        // 소모품 카테고리 체크 및 사용 로직 (기존 기능 보존)
        if (slot.IsEmpty || slot.item.category != ItemCategory.Consume || hero == null) return;

        slot.item.Use(hero);
        sourceInv.RemoveFromSlot(index, 1);
        
        hero.Stat?.StatCalculate();
        OnEquipmentChanged?.Invoke();
    }

    public void EquipFromDrag(IInventory sourceInv, int invIndex, int charSlotIndex)
    {
        var hero = AdminTestTool.testHero;
        var slot = sourceInv.GetSlot(invIndex);
        if (hero == null || slot.IsEmpty || slot.item is not AccessoryItem accItem) return;

        if (hero.Data.Equips[charSlotIndex]?.originItem != null)
        {
            UnequipToInventory(hero, charSlotIndex);
        }

        var newEquip = accItem.equipmentData.Clone(accItem);
        newEquip.originItem = accItem;
        hero.Data.Equips[charSlotIndex] = newEquip;

        sourceInv.RemoveFromSlot(invIndex, 1);
        UpdateHeroStats(hero);
    }

    public void UnequipToInventory(BaseCharacter target, int slotIdx)
    {
        var eq = target.Data.Equips[slotIdx];
        if (eq?.originItem == null) return;

        // [기존 로직 유지] UIService를 통해 현재 어떤 창이 열려있는지 확인하여 귀환 위치 결정
        IInventory targetInv = (_uiService.IsInventoryWindowActive || _uiService.IsAccessoryBagActive) 
                               ? _totalInventory : _expeditionInventory;

        int remain = targetInv.AddItemAuto(eq.originItem, 1);
        
        if (remain > 0)
        {
            var fallback = (targetInv == _totalInventory) ? (IInventory)_expeditionInventory : _totalInventory;
            fallback.AddItemAuto(eq.originItem, remain);
        }

        target.Data.Equips[slotIdx] = null;
        UpdateHeroStats(target);
    }

    public bool AutoEquip(IInventory sourceInv, int invIndex)
    {
        var hero = AdminTestTool.testHero;
        var slot = sourceInv.GetSlot(invIndex);
        if (hero == null || slot.item is not AccessoryItem accItem) return false;

        for (int i = 0; i < hero.Data.Equips.Length; i++)
        {
            if (hero.Data.Equips[i] == null || hero.Data.Equips[i].originItem == null)
            {
                hero.Data.Equips[i] = accItem.equipmentData.Clone(accItem);
                hero.Data.Equips[i].originItem = accItem;
                sourceInv.RemoveFromSlot(invIndex, 1);
                UpdateHeroStats(hero);
                return true;
            }
        }
        return false;
    }

    private void UpdateHeroStats(BaseCharacter hero)
    {
        hero.Stat?.StatCalculate();
        OnEquipmentChanged?.Invoke();
    }
}