using UnityEngine;

/// <summary>
/// 스테이지 타입 하나의 시각/씬 설정을 담는 ScriptableObject.
/// StageType별로 하나씩 에셋을 생성해 MapGeneratorConfig.StageConfigs에 등록한다.
///
/// 생성 위치: Assets 우클릭 → Create → Bond → StageConfig
/// </summary>
[CreateAssetMenu(menuName = "Bond/StageConfig")]
public class StageConfig : ScriptableObject
{
    public StageType Type;          // 이 설정이 대응하는 스테이지 타입

    public string DisplayName;      // UI에 표시할 이름 (예: "일반 전투", "엘리트")
    public Sprite Icon;             // 맵 노드에 표시할 아이콘 스프라이트 (직접 참조)
    public string IconAddress;      // Addressables로 동적 로드할 때 사용하는 아이콘 주소
    public string SceneAddress;     // Addressables로 로드할 스테이지 씬 주소
    public Color NodeColor;         // 맵 노드 배경색 (스테이지 타입을 색상으로 구분)
}
