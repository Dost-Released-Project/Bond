using UnityEngine;
using System;
using System.Collections.Generic;
using System.IO;
using Bond.Persistence;

public enum ResourceType { Frontier, Wood, Ore }

public class ResourceManager
{
    private Dictionary<ResourceType, ResourceData> _resources = new();
    public event Action<ResourceType, ResourceData> OnResourceChanged;

    // 💥 최초 시작 여부를 판단하는 동적 상태 변수
    private bool _isFirstStart = true;

    public ResourceManager()
    {
        _resources[ResourceType.Frontier] = new ResourceData("개척 데이터", 10000);
        _resources[ResourceType.Wood] = new ResourceData("목재", 1000);
        _resources[ResourceType.Ore] = new ResourceData("광석", 1000);
        
        var loadData = new ResourceSaveData();
        string saveKey = loadData.Key;

        if (SaveLoadSystem.HasSave(saveKey))
        {
            try 
            {
                SaveLoadSystem.Load(loadData);
                
                _resources[ResourceType.Frontier].Max = loadData.frontierMax;
                _resources[ResourceType.Wood].Max = loadData.woodMax;
                _resources[ResourceType.Ore].Max = loadData.oreMax;
            
                _resources[ResourceType.Frontier].Current = loadData.frontierCur;
                _resources[ResourceType.Wood].Current = loadData.woodCur;
                _resources[ResourceType.Ore].Current = loadData.oreCur;

                // 💥 세이브가 존재하므로 최초 시작이 아님을 마킹
                _isFirstStart = false;

                Debug.Log("ResourceManager: 기존 데이터 로드 완료 (최초 지급 스킵)");
            }
            catch (Exception e)
            {
                Debug.LogError($"ResourceManager: 로드 중 오류 발생 - {e.Message}");
            }
        }
        else 
        {
            Debug.Log("ResourceManager: 기존 세이브 없음. 순수 신규 게임으로 인지.");
        }

        // 💥 [핵심 기획 반영] 순수 새 게임 시작일 때만 단 한 번 보너스 재화 다이렉트 할당
        if (_isFirstStart)
        {
            Debug.Log("<color=yellow>[최초 실행]</color> 새 게임 시작 보너스 자원을 1회 한정 지급합니다.");
            
            // 초기 세팅치 최대 보유량 범위 안에서 안전하게 추가 정산되도록 오버로드 연산
            Admin_AddAllResources(10000); 
            
            // 지급 후 다음 프레임이나 재실행 시 다시 호출되지 않도록 즉시 false 락 처리
            _isFirstStart = false;
        }
    }

    public int FrontierData => _resources[ResourceType.Frontier].Current;
    public int Wood => _resources[ResourceType.Wood].Current;
    public int Ore => _resources[ResourceType.Ore].Current;

    public void ExpandCapacities(int fAdd, int wAdd, int oAdd)
    {
        _resources[ResourceType.Frontier].Max = fAdd;
        _resources[ResourceType.Wood].Max = wAdd;
        _resources[ResourceType.Ore].Max = oAdd;
        NotifyAll();
    }

    public void AddResource(ResourceType type, int amount)
    {
        var res = _resources[type];
        res.Current = Mathf.Clamp(res.Current + amount, 0, res.Max);
        OnResourceChanged?.Invoke(type, res);
        NotifyAll();
    }

    public void AddFrontierData(int amount) => AddResource(ResourceType.Frontier, amount);
    public void AddMaterials(int wood, int ore) { AddResource(ResourceType.Wood, wood); AddResource(ResourceType.Ore, ore); }
    
    public void Admin_AddAllResources(int amount)
    {
        foreach (var type in _resources.Keys) AddResource(type, amount);
    }

    public bool ConsumeResources(int frontier, int wood, int ore)
    {
        if (FrontierData >= frontier && Wood >= wood && Ore >= ore)
        {
            _resources[ResourceType.Frontier].Current -= frontier;
            _resources[ResourceType.Wood].Current -= wood;
            _resources[ResourceType.Ore].Current -= ore;
            NotifyAll();
            return true;
        }
        return false;
    }

    private void NotifyAll()
    {
        foreach (var res in _resources) OnResourceChanged?.Invoke(res.Key, res.Value);
        
        // 세이브 데이터 규격 조립
        var resSave = new ResourceSaveData {
            frontierCur = FrontierData, woodCur = Wood, oreCur = Ore,
            frontierMax = _resources[ResourceType.Frontier].Max, woodMax = _resources[ResourceType.Wood].Max, oreMax = _resources[ResourceType.Ore].Max
        };
        SaveLoadSystem.Save(resSave);
    }
}