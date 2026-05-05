using System;
using UnityEngine;

public class CharacterItemService
{
    public event Action OnEquipmentChanged;

    public void UseItem(BaseCharacter hero, IInventory sourceInv, int index)
    {
        var slot = sourceInv.GetSlot(index);
        if (slot.IsEmpty || slot.item.category != ItemCategory.Consume || hero == null) return;

        slot.item.Use(hero);
        sourceInv.RemoveFromSlot(index, 1);
        UpdateHeroStats(hero);
    }

    // [개선] 장착 해제 시 인벤토리 공간 확인 (증발 방지)
    public bool UnequipToInventory(BaseCharacter target, int slotIdx, IInventory targetInv)
    {
        var eq = target.Data.Equips[slotIdx];
        if (eq?.originItem == null || targetInv == null) return false;

        // 1. 아이템을 먼저 넣어보고 남는 개수가 있는지 확인
        int remain = targetInv.AddItemAuto(eq.originItem, 1);
        
        // 2. remain이 0보다 크면 들어갈 자리가 없다는 뜻 -> 해제 취소
        if (remain > 0) 
        {
            Debug.LogWarning("인벤토리에 빈 공간이 없어 장비를 해제할 수 없습니다.");
            return false;
        }

        // 3. 성공적으로 들어갔을 때만 슬롯 비우기
        target.Data.Equips[slotIdx] = null;
        UpdateHeroStats(target);
        return true;
    }

    // [추가] 장착 장신구를 드래그하여 인벤토리의 '특정 슬롯'에 놓았을 때 처리 (해제 및 스왑)
    public bool UnequipToInventorySlot(BaseCharacter target, int slotIdx, IInventory targetInv, int targetSlotIdx)
    {
        if (target?.Data?.Equips == null || slotIdx < 0 || slotIdx >= target.Data.Equips.Length) return false;
        var eq = target.Data.Equips[slotIdx];
        if (eq?.originItem == null || targetInv == null) return false;

        var targetSlot = targetInv.GetSlot(targetSlotIdx);

        // 1. 목표 인벤토리 슬롯이 비어있으면 바로 안착
        if (targetSlot.IsEmpty)
        {
            targetInv.AddItemAt(targetSlotIdx, eq.originItem, 1);
            target.Data.Equips[slotIdx] = null;
            UpdateHeroStats(target);
            return true;
        }

        // 2. 목표 인벤토리 슬롯에 다른 장신구가 있으면 서로 교체(스왑)
        if (targetSlot.item is AccessoryItem accItem)
        {
            var oldEquip = eq;
            targetInv.RemoveFromSlot(targetSlotIdx, 1);

            var newEquip = accItem.equipmentData.Clone(accItem);
            newEquip.originItem = accItem;
            target.Data.Equips[slotIdx] = newEquip;

            targetInv.AddItemAt(targetSlotIdx, oldEquip.originItem, 1);
            UpdateHeroStats(target);
            return true;
        }

        // 3. 만약 교체 불가능한 템이 들어있다면 빈 공간 자동 탐색으로 해제 시도
        return UnequipToInventory(target, slotIdx, targetInv);
    }

    // [추가] 장착 장신구를 화면 바깥으로 드래그해서 버렸을 때 처리
    public void DiscardEquipment(BaseCharacter target, int slotIdx)
    {
        if (target?.Data?.Equips == null || slotIdx < 0 || slotIdx >= target.Data.Equips.Length) return;
        var eq = target.Data.Equips[slotIdx];
        if (eq == null) return;

        Debug.Log($"[장비 파괴] 영역 밖에 드롭하여 장착 중인 {eq.originItem.itemName}을(를) 버렸습니다.");
        target.Data.Equips[slotIdx] = null;
        UpdateHeroStats(target);
    }

    // [추가] 드래그를 통한 장착 로직
    public void EquipFromDrag(IInventory sourceInv, int invIndex, int charSlotIndex)
    {
        var hero = AdminTestTool.testHero;
        var slot = sourceInv.GetSlot(invIndex);
        if (hero == null || slot.IsEmpty || slot.item is not AccessoryItem accItem) return;

        // 기존 장비가 있다면 먼저 해제 시도 (위의 증발 방지 로직 활용)
        if (hero.Data.Equips[charSlotIndex]?.originItem != null)
        {
            bool unequipSuccess = UnequipToInventory(hero, charSlotIndex, sourceInv);
            if (!unequipSuccess) return; // 공간 없으면 장착 교체 불가
        }

        // 신규 장비 장착
        var newEquip = accItem.equipmentData.Clone(accItem);
        newEquip.originItem = accItem;
        hero.Data.Equips[charSlotIndex] = newEquip;

        sourceInv.RemoveFromSlot(invIndex, 1);
        UpdateHeroStats(hero);
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