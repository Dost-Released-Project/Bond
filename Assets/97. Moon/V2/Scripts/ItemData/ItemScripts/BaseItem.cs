using System;
using UnityEngine;

public enum ItemCategory { Consume, Accessories }

public abstract class BaseItem : ScriptableObject
{
    public string id;
    public string itemName;
    public ItemCategory category;
    public Sprite icon;

    [Header("Stack Settings")]
    [Tooltip("전체 창고에서 가질 수 있는 아이템의 '총합' 최대치")]
    public int totalGlobalMax = 10; 
    
    [Tooltip("탐사 인벤토리의 '한 슬롯'에 담길 수 있는 최대치")]
    public int expeditionSlotMax = 2; 

    public virtual void Use(BaseCharacter target) { }
}

[Serializable]
public class InventorySlot
{
    public BaseItem item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;
    
    // IsFull 로직은 인벤토리 타입에 따라 달라지므로 데이터 클래스에서는 제거하거나 
    // 아래처럼 범용적으로 체크할 수 있게 변경합니다.
    public bool IsFull(int limit) => item != null && quantity >= limit;

    public void Clear() { item = null; quantity = 0; }
}