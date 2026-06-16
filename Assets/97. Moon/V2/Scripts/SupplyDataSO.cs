using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public struct SupplyItemPair
{
    public string itemId;
    public int count;
}

public class SupplyDataSO : BaseSO
{
    [Header("보급 설정")]
    [Tooltip("해당 보급 묶음이 등장할 독립 가중치 확률 (예: 40.0)")]
    [SerializeField] private float _rate;
    
    [Tooltip("이 묶음에 포함된 아이템과 수량 리스트")]
    [SerializeField] private List<SupplyItemPair> _bundleItems = new();

    public float Rate => _rate;
    public List<SupplyItemPair> BundleItems => _bundleItems;

    public void SetData(string id, string displayName, float rate, List<SupplyItemPair> items)
    {
        base.Initialize(id, displayName, "");
        _rate = rate;
        _bundleItems = items;
    }
}