using UnityEngine;

public static class BuildingFactory
{
    public static BuildingObject Create(Transform parent, BuildingData data, ISettlementManager manager)
    {
        GameObject buildingGo = new GameObject($"Building_{data.DisplayName}");
        buildingGo.transform.SetParent(parent);
        buildingGo.transform.localPosition = new Vector3(0, 0.1f, 0); 
        buildingGo.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
        buildingGo.transform.localRotation = Quaternion.Euler(0, 0, 0); 

        var sr = buildingGo.AddComponent<SpriteRenderer>();
        sr.sprite = data.buildingSprite; 
        sr.drawMode = SpriteDrawMode.Simple;

        var col = buildingGo.AddComponent<BoxCollider>();
        if (sr.sprite != null)
        {
            col.size = new Vector3(sr.bounds.size.x, sr.bounds.size.y, 2.0f);
            col.center = new Vector3(0, 0, -0.5f);
        }

        // 💥 [핵심 분리] 생성되는 순간 비주얼 컴포넌트와 인터랙션 부품을 독립 장착합니다.
        buildingGo.AddComponent<BuildingVisualAnims>();
        buildingGo.AddComponent<BuildingInteractionCounter>();

        // 핵심 기능 스크립트 연결 및 배포
        var bObj = buildingGo.AddComponent<BuildingObject>();
        bObj.Initialize(data, manager);

        return bObj;
    }
}