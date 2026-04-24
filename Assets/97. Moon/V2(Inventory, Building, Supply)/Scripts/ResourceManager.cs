using UnityEngine;
using System;

public class ResourceManager
{
    public int FrontierData { get; private set; }
    public int Wood { get; private set; }
    public int Ore { get; private set; }

    // UI 갱신 이벤트 (개척 데이터, 목재, 광물 순)
    public event Action<int, int, int> OnResourcesChanged;

    public void AddFrontierData(int amount)
    {
        FrontierData = Mathf.Clamp(FrontierData + amount, 0, 99999);
        NotifyChange();
    }

    public void AddMaterials(int wood, int ore)
    {
        Wood += wood;
        Ore += ore;
        NotifyChange();
    }

    public bool ConsumeResources(int frontier, int wood, int ore)
    {
        if (FrontierData >= frontier && Wood >= wood && Ore >= ore)
        {
            FrontierData -= frontier;
            Wood -= wood;
            Ore -= ore;
            NotifyChange();
            return true;
        }
        return false;
    }

    private void NotifyChange() => OnResourcesChanged?.Invoke(FrontierData, Wood, Ore);
}