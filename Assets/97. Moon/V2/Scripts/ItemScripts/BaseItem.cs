using System;
using UnityEngine;

public enum ItemCategory { Consume, Accessories }

/// <summary>
/// [D] Pure Data: 모든 아이템의 기반이 되는 SO.
/// BaseSO를 상속받아 DataBaseSO 시스템과 통합됩니다.
/// </summary>
public abstract class BaseItem : BaseSO
{
    // 기존 코드와의 호환성을 위한 프로퍼티
    public string id => Id;
    public string itemName => DisplayName;

    public ItemCategory category;
    public Sprite icon;

    [Header("Stack Settings")]
    [Tooltip("전체 창고에서 가질 수 있는 아이템의 '총합' 최대치")]
    public int totalGlobalMax = 10; 
    
    [Tooltip("탐사 인벤토리의 '한 슬롯'에 담길 수 있는 최대치")]
    public int expeditionSlotMax = 2; 

    public virtual void Use(BaseCharacter target) { }

    /// <summary>
    /// 파서에서 데이터를 주입하기 위한 메서드.
    /// </summary>
    public virtual void SetBaseData(string id, string name, string desc, ItemCategory cat, int globalMax, int slotMax)
    {
        base.Initialize(id, name, desc);
        this.category = cat;
        this.totalGlobalMax = globalMax;
        this.expeditionSlotMax = slotMax;
    }
}

[Serializable]
public class InventorySlot
{
    public BaseItem item;
    public int quantity;

    public bool IsEmpty => item == null || quantity <= 0;
    
    public bool IsFull(int limit) => item != null && quantity >= limit;

    public void Clear() { item = null; quantity = 0; }
}
