using System.Collections.Generic;
using UnityEngine;

public interface ISettlementManager
{
    /// <summary>개척 데이터 및 자원(A, B, C)을 소모하여 건물 건설</summary>
    /// <param name="resources">필요 자원 리스트 (Placeholder 자원 포함)</param>
    void ConstructBuilding(int slotIndex, BuildingType type, List<ResourceData> resources);

    /// <summary>건물 업그레이드 (자원 소모 포함)</summary>
    void UpgradeBuilding(int slotIndex, List<ResourceData> resources);

    /// <summary>건물 기능 실행 (체력 회복, 스트레스 감소, 방어구 강화 등)</summary>
    /// <remarks>실제 수치 연산 외의 연출은 Debug.Log로 처리</remarks>
    void ExecuteBuildingFunction(int slotIndex, BaseCharacter target);
}