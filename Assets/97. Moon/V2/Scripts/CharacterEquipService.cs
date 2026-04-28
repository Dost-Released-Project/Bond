using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

public class CharacterEquipService
{
    private ITotalInventory _totalInventory;
    [Inject] public void Construct(ITotalInventory total) => _totalInventory = total;

    public bool AutoEquip(IInventory sourceInv, int invIndex)
    {
        var hero = AdminTestTool.testHero;
        var slot = sourceInv.GetSlot(invIndex);
        if (hero == null || slot.item is not AccessoryItem accItem) return false;

        for (int i = 0; i < hero.equip.Length; i++)
        {
            if (hero.equip[i] == null)
            {
                hero.equip[i] = accItem.equipmentData.Clone(accItem);
                sourceInv.RemoveFromSlot(invIndex, 1);
                hero.TryGetComponent<Stat>(out var stat); stat?.StatCalculate();
                return true;
            }
        }
        return false;
    }
    
    public void EquipFromDrag(IInventory sourceInv, int invIndex, int charSlotIndex)
    {
        var hero = AdminTestTool.testHero;
        var slot = sourceInv.GetSlot(invIndex);
    
        if (hero == null || slot.IsEmpty || slot.item.category != ItemCategory.Accessories) return;

        // 수정 포인트 1: 장비가 '실제로' 있을 때만 Unequip 호출
        // 단순히 null 체크뿐만 아니라 originItem이 살아있는지 확인
        if (hero.equip[charSlotIndex] != null && hero.equip[charSlotIndex].originItem != null) 
        {
            UnequipToInventory(hero, charSlotIndex, _totalInventory);
        }
        else
        {
            // 만약 데이터가 꼬여서 껍데기(null은 아닌데 origin이 없음)만 있다면 그냥 강제로 비움
            hero.equip[charSlotIndex] = null;
        }
    
        var accItem = slot.item as AccessoryItem;
        if (accItem == null) return;

        var newEquip = accItem.equipmentData.Clone(accItem);
        newEquip.originItem = accItem; 
    
        hero.equip[charSlotIndex] = newEquip;
    
        sourceInv.RemoveFromSlot(invIndex, 1);
        hero.TryGetComponent<Stat>(out var stat); stat?.StatCalculate();
    }

    public void UnequipToInventory(BaseCharacter target, int slotIdx, ITotalInventory total)
    {
        // 수정 포인트 2: 여기서도 이중 방어
        var targetEquip = target.equip[slotIdx];
    
        // 데이터가 아예 없으면 조용히 리턴 (Warning 로그 제거)
        if (targetEquip == null) return;

        // 데이터는 있는데 되돌릴 원본 아이템(SO) 정보가 없다면
        if (targetEquip.originItem == null)
        {
            target.equip[slotIdx] = null; // 인벤토리엔 못 넣어도 슬롯은 비워줌
            return;
        }

        total.AddItemAuto(targetEquip.originItem, 1);
        target.equip[slotIdx] = null;
    
        target.TryGetComponent<Stat>(out var stat); stat?.StatCalculate();
    }
}