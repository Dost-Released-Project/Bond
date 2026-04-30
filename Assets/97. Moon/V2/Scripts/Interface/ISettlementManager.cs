using System.Collections.Generic;
using UnityEngine;

public interface ISettlementManager
{
    /// <summary>개척 데이터 및 자원(A, B, C)을 소모하여 건물 건설</summary>
    /// <param name="resources">필요 자원 리스트 (Placeholder 자원 포함)</param>
    public void BuildInSlot(int slotIndex, BuildingData data); // 현재: 필요 자원을 빌딩 데이터에서 접근.
    // void ConstructBuilding(int slotIndex, BuildingType type, List<ResourceData> resources); // 과거: 필요 자원을 리스트로 접근.

    /// <summary>건물 업그레이드 (자원 소모 포함)</summary>
    public void UpgradeBuilding(BuildingObject building); // 현재: 클릭을 통해 건물 위치 파악, 빌딩 오브젝트 하나로 접근.
    // void UpgradeBuilding(int slotIndex, List<ResourceData> resources); // 과거: 인덱스 접근 및 필요 자원 리스트로 접근

    /// <summary>건물 기능 실행 (체력 회복, 스트레스 감소, 방어구 강화 등)</summary>
    /// <remarks>실제 수치 연산 외의 연출은 Debug.Log로 처리</remarks>
    public void OnBuildingClicked(BuildingObject building); // 현재: 따로 선택된 플레이어에게 접근하는 로직 작성, 빌딩 오브젝트만 하나로 접근.
    void ExecuteBuildingFunction(int slotIndex, BaseCharacter target); // 과거: 인덱스와 타겟 데이터로 접근.
}