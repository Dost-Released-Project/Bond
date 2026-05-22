using UnityEngine;
using UnityEngine.UIElements;

public class BuildingTooltipView : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _tooltipContainer;
    private Label _lblTargetName;
    private Label _lblTargetLevel;
    private Label _lblDescription;
    private Label _lblEffect;
    private Label _lblUpgradeInfo;
    private Label _lblGuide;

    private void OnEnable()
    {
        _root = GetComponent<UIDocument>().rootVisualElement;
        _root.pickingMode = PickingMode.Ignore;

        _tooltipContainer = new VisualElement();
        _tooltipContainer.style.position = Position.Absolute;
        _tooltipContainer.style.visibility = Visibility.Hidden;
        _tooltipContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);
        
        _tooltipContainer.style.borderLeftWidth = 1; _tooltipContainer.style.borderRightWidth = 1;
        _tooltipContainer.style.borderTopWidth = 1; _tooltipContainer.style.borderBottomWidth = 1;
        _tooltipContainer.style.borderLeftColor = new Color(0.65f, 0.5f, 0.3f);
        _tooltipContainer.style.borderRightColor = new Color(0.65f, 0.5f, 0.3f);
        _tooltipContainer.style.borderTopColor = new Color(0.65f, 0.5f, 0.3f);
        _tooltipContainer.style.borderBottomColor = new Color(0.65f, 0.5f, 0.3f);
        
        _tooltipContainer.style.borderTopLeftRadius = 6; _tooltipContainer.style.borderTopRightRadius = 6;
        _tooltipContainer.style.borderBottomLeftRadius = 6; _tooltipContainer.style.borderBottomRightRadius = 6;
        
        _tooltipContainer.style.paddingLeft = 14; _tooltipContainer.style.paddingRight = 14;
        _tooltipContainer.style.paddingTop = 14; _tooltipContainer.style.paddingBottom = 14;
        
        // =========================================================================
        // 🎯 [UI Toolkit 핏 교정] 고정 너비를 제거하고 글자 수에 맞게 박스가 유연하게 늘어나도록 설정
        // =========================================================================
        _tooltipContainer.style.width = StyleKeyword.Auto;
        _tooltipContainer.style.height = StyleKeyword.Auto;
        _tooltipContainer.style.minWidth = 280; // 최소 이 정도 너비는 유지하여 안정감 확보
        _tooltipContainer.style.maxWidth = 400; // 너무 무한정 늘어나서 화면을 가리는 현상 방지
        _tooltipContainer.pickingMode = PickingMode.Ignore;

        _lblTargetName = CreateTooltipLabel(18, Color.white, true);
        _lblTargetLevel = CreateTooltipLabel(13, new Color(0.6f, 0.8f, 1f));
        
        _lblDescription = CreateTooltipLabel(12, new Color(0.85f, 0.85f, 0.85f));
        _lblDescription.style.whiteSpace = WhiteSpace.Normal; // 자동 줄바꿈
        _lblDescription.style.marginTop = 6;

        // 💥 효과 및 다음 레벨 텍스트 레이블에도 자동 줄바꿈(Normal) 속성을 확실하게 주입하고 폰트 크기를 가독성 있게 최적화
        _lblEffect = CreateTooltipLabel(12, new Color(0.3f, 0.9f, 0.3f));
        _lblEffect.style.whiteSpace = WhiteSpace.Normal;
        _lblEffect.style.marginTop = 6;
        
        _lblUpgradeInfo = CreateTooltipLabel(11, new Color(0.9f, 0.6f, 0.2f));
        _lblUpgradeInfo.style.whiteSpace = WhiteSpace.Normal;
        _lblUpgradeInfo.style.marginTop = 6;

        _lblGuide = CreateTooltipLabel(11, new Color(0.5f, 0.5f, 0.5f));
        _lblGuide.style.marginTop = 10;
        _lblGuide.text = "💡 좌클릭: 상호작용 | 우클릭: 업그레이드";

        _tooltipContainer.Add(_lblTargetName); _tooltipContainer.Add(_lblTargetLevel);
        _tooltipContainer.Add(_lblDescription); _tooltipContainer.Add(_lblEffect);
        _tooltipContainer.Add(_lblUpgradeInfo); _tooltipContainer.Add(_lblGuide);

        _root.Add(_tooltipContainer);
    }

    private Label CreateTooltipLabel(int fontSize, Color color, bool isBold = false)
    {
        var label = new Label();
        label.style.fontSize = fontSize;
        label.style.color = color;
        if (isBold) label.style.unityFontStyleAndWeight = FontStyle.Bold;
        label.pickingMode = PickingMode.Ignore;
        return label;
    }

    public void ShowTooltip(BuildingObject building, Vector2 mousePosition)
    {
        if (building == null || building.Data == null) return;

        var data = building.Data;
        int curLevel = building.CurrentLevel;
        if (curLevel > data.levels.Count) curLevel = data.levels.Count;

        _lblTargetName.text = data.DisplayName;
        _lblTargetLevel.text = $"레벨: {curLevel} / {data.levels.Count}";
        _lblDescription.text = string.IsNullOrEmpty(data.Description) ? "마을의 주요 건물입니다." : data.Description;

        // 1레벨부터 현재 레벨까지 누적 합산 계산
        int totalMaterialCap = 0;
        int totalFrontierCap = 0;
        int totalSlotExpansion = 0;
        int totalEffectValue = 0;

        for (int i = 1; i <= curLevel; i++)
        {
            var levelData = data.GetLevelData(i);
            if (levelData.level != 0)
            {
                totalMaterialCap += levelData.materialCapAdd;
                totalFrontierCap += levelData.frontierCapAdd;
                totalSlotExpansion += levelData.slotExpansion;
                totalEffectValue += levelData.effectValue;
            }
        }

        string effectText = "[현재 적용 효과]\n";
        if (totalMaterialCap > 0) effectText += $"- 자원 보관 한도: {totalMaterialCap} 증가\n";
        if (totalFrontierCap > 0) effectText += $"- 개척 가능 한도: {totalFrontierCap} 증가\n";
        if (totalSlotExpansion > 0) effectText += $"- 탐사 인벤토리 슬롯: {totalSlotExpansion}칸 증가\n";
        if (totalEffectValue > 0) effectText += $"- 건물 고유 효과 수치: {totalEffectValue} 증가\n";
        
        if (data.buildingType == BuildingType.Smithy || data.name.Contains("Smithy"))
        {
            effectText += $"- 장비 강화 최대 제한: {curLevel}단계\n";
        }
        _lblEffect.text = effectText;

        // 다음 레벨 수치 누적 가이드라인 맵핑
        int nextLevel = curLevel + 1;
        if (nextLevel <= data.levels.Count)
        {
            var nextLevelData = data.GetLevelData(nextLevel);

            int nextMaterialCap = totalMaterialCap + nextLevelData.materialCapAdd;
            int nextFrontierCap = totalFrontierCap + nextLevelData.frontierCapAdd;
            int nextSlotExpansion = totalSlotExpansion + nextLevelData.slotExpansion;
            int nextEffectValue = totalEffectValue + nextLevelData.effectValue;

            string upText = "[업그레이드 시 최종 변경값]\n";
            bool hasChanges = false;

            if (nextLevelData.materialCapAdd > 0) { upText += $" 자원 보관 한도: {totalMaterialCap} ➔ {nextMaterialCap} 증가\n"; hasChanges = true; }
            if (nextLevelData.frontierCapAdd > 0) { upText += $" 개척 가능 한도: {totalFrontierCap} ➔ {nextFrontierCap} 증가\n"; hasChanges = true; }
            if (nextLevelData.slotExpansion > 0) { upText += $" 인벤토리 슬롯: {totalSlotExpansion}칸 ➔ {nextSlotExpansion}칸 증가\n"; hasChanges = true; }
            if (nextLevelData.effectValue > 0) { upText += $" 효과 고유 수치: {totalEffectValue} ➔ {nextEffectValue} 증가\n"; hasChanges = true; }
            if (data.buildingType == BuildingType.Smithy || data.name.Contains("Smithy")) { upText += $" 장비 최고 강화 한도: {curLevel}단계 ➔ {nextLevel}단계 제한 확장\n"; hasChanges = true; }

            if (!hasChanges) upText += $" 건물의 전반적인 기능성 및 내구도 증가\n";
            _lblUpgradeInfo.text = upText;
            _lblUpgradeInfo.style.display = DisplayStyle.Flex;
        }
        else
        {
            _lblUpgradeInfo.text = "최고 레벨에 도달하여 추가 업그레이드가 불가능합니다.";
            _lblUpgradeInfo.style.display = DisplayStyle.Flex;
        }

        _tooltipContainer.style.left = new StyleLength(new Length(mousePosition.x + 15, LengthUnit.Pixel));
        _tooltipContainer.style.top = new StyleLength(new Length(mousePosition.y + 15, LengthUnit.Pixel));
        _tooltipContainer.style.visibility = Visibility.Visible;
        _tooltipContainer.BringToFront();
    }

    public void HideTooltip()
    {
        if (_tooltipContainer != null)
            _tooltipContainer.style.visibility = Visibility.Hidden;
    }
}