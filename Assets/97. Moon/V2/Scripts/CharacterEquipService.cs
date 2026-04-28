using UnityEngine;
using VContainer;
using System;

public class CharacterEquipService
{
    private ITotalInventory _totalInventory;
    private IExpeditionInventory _expeditionInventory;

    // 이벤트 추가: 장착 상태가 변하면 UI들이 이를 듣고 갱신함
    public event Action OnEquipmentChanged;

    [Inject]
    public void Construct(ITotalInventory total, IExpeditionInventory exp)
    {
        _totalInventory = total;
        _expeditionInventory = exp;
    }

    public void EquipFromDrag(IInventory sourceInv, int invIndex, int charSlotIndex)
    {
        var hero = AdminTestTool.testHero;
        var slot = sourceInv.GetSlot(invIndex);
        if (hero == null || slot.IsEmpty || slot.item is not AccessoryItem accItem) return;

        // 기존 장비가 있다면 해제 (현재 열린 UI 상황에 맞춰 귀환)
        if (hero.equip[charSlotIndex]?.originItem != null)
        {
            UnequipToInventory(hero, charSlotIndex);
        }

        // 새 장비 장착
        var newEquip = accItem.equipmentData.Clone(accItem);
        newEquip.originItem = accItem;
        hero.equip[charSlotIndex] = newEquip;

        sourceInv.RemoveFromSlot(invIndex, 1);
        UpdateHeroStats(hero);
    }

    public void UnequipToInventory(BaseCharacter target, int slotIdx)
    {
        var eq = target.equip[slotIdx];
        if (eq?.originItem == null) return;

        // [귀환 로직] 전체 인벤토리나 장신구 가방이 켜져있으면 전체로, 아니면 탐사로
        IInventory targetInv = (InventoryView.IsWindowActive || AccessoryBagView.IsWindowActive) 
                               ? _totalInventory : _expeditionInventory;

        int remain = targetInv.AddItemAuto(eq.originItem, 1);
        
        // 목적지가 꽉 찼을 경우를 대비한 백업
        if (remain > 0)
        {
            var fallback = (targetInv == _totalInventory) ? (IInventory)_expeditionInventory : _totalInventory;
            fallback.AddItemAuto(eq.originItem, remain);
        }

        target.equip[slotIdx] = null;
        UpdateHeroStats(target);
    }

    public bool AutoEquip(IInventory sourceInv, int invIndex)
    {
        var hero = AdminTestTool.testHero;
        var slot = sourceInv.GetSlot(invIndex);
        if (hero == null || slot.item is not AccessoryItem accItem) return false;

        for (int i = 0; i < hero.equip.Length; i++)
        {
            if (hero.equip[i] == null || hero.equip[i].originItem == null)
            {
                hero.equip[i] = accItem.equipmentData.Clone(accItem);
                hero.equip[i].originItem = accItem;
                sourceInv.RemoveFromSlot(invIndex, 1);
                UpdateHeroStats(hero);
                return true;
            }
        }
        return false;
    }

    private void UpdateHeroStats(BaseCharacter hero)
    {
        hero.TryGetComponent<Stat>(out var stat);
        stat?.StatCalculate();
        OnEquipmentChanged?.Invoke(); // 모든 UI에 알림
    }
}