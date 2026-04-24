using UnityEngine;
using System;

public class ResourceManager
{
    // 현재 자원
    public int FrontierData { get; private set; }
    public int Wood { get; private set; }
    public int Ore { get; private set; }

    // 자원 상한치 (창고 업그레이드로 확장됨)
    public int MaxFrontierData { get; private set; } = 1000;
    public int MaxWood { get; private set; } = 100;
    public int MaxOre { get; private set; } = 100;

    public event Action<int, int, int> OnResourcesChanged;

    // --- 자원 상한 확장 로직 ---
    public void ExpandCapacities(int frontierAdd, int woodAdd, int oreAdd)
    {
        MaxFrontierData += frontierAdd;
        MaxWood += woodAdd;
        MaxOre += oreAdd;
        NotifyChange();
    }

    // --- 자원 추가 (Clamping 적용) ---
    public void AddFrontierData(int amount)
    {
        FrontierData = Mathf.Clamp(FrontierData + amount, 0, MaxFrontierData);
        NotifyChange();
    }

    public void AddMaterials(int wood, int ore)
    {
        Wood = Mathf.Clamp(Wood + wood, 0, MaxWood);
        Ore = Mathf.Clamp(Ore + ore, 0, MaxOre);
        NotifyChange();
    }

    // --- 관리자 모드 (테스트용) ---
    public void Admin_AddAllResources(int amount)
    {
        AddFrontierData(amount);
        AddMaterials(amount, amount);
        Debug.Log($"<color=yellow>[Admin]</color> 모든 자원을 {amount}만큼 추가했습니다.");
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