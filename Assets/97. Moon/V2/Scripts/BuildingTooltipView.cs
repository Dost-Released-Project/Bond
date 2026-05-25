using UnityEngine;
using UnityEngine.UIElements;

public class BuildingTooltipView : MonoBehaviour
{
    private VisualElement _root;
    private VisualElement _tooltipContainer;
    
    // 💥 [구조 변경] 우측 상단 배치를 위해 이름과 횟수를 감쌀 상단 타이틀 바 컨테이너
    private VisualElement _titleBarContainer;
    private Label _lblTargetName;
    private Label _lblUseCount; // 📊 <현재 사용 횟수 / 최대 사용 횟수> 전담 레이블
    
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
        
        _tooltipContainer.style.width = StyleKeyword.Auto;
        _tooltipContainer.style.height = StyleKeyword.Auto;
        _tooltipContainer.style.minWidth = 280; 
        _tooltipContainer.style.maxWidth = 400; 
        _tooltipContainer.pickingMode = PickingMode.Ignore;

        // =========================================================================
        // 🎯 [UI 레이아웃 교정] 이름은 왼쪽, 사용 횟수는 우측 구석 상단에 배치되도록 정렬
        // =========================================================================
        _titleBarContainer = new VisualElement();
        _titleBarContainer.style.flexDirection = FlexDirection.Row;
        _titleBarContainer.style.justifyContent = Justify.SpaceBetween; // 양쪽 끝 배치 규칙
        _titleBarContainer.style.alignItems = Align.Center;

        _lblTargetName = CreateTooltipLabel(18, Color.white, true);
        
        // 사용 제한 횟수 레이블 생성 (가독성을 위해 화사한 연노랑 컬러 배정)
        _lblUseCount = CreateTooltipLabel(14, new Color(1.0f, 0.85f, 0.4f), true);

        // 타이틀 바 컨테이너에 결합
        _titleBarContainer.Add(_lblTargetName);
        _titleBarContainer.Add(_lblUseCount);
        
        _lblTargetLevel = CreateTooltipLabel(13, new Color(0.6f, 0.8f, 1f));
        _lblTargetLevel.style.marginTop = 2; // 타이틀 바 바로 아래 간격 살짝 조정
        
        _lblDescription = CreateTooltipLabel(12, new Color(0.85f, 0.85f, 0.85f));
        _lblDescription.style.whiteSpace = WhiteSpace.Normal; 
        _lblDescription.style.marginTop = 6;

        _lblEffect = CreateTooltipLabel(12, new Color(0.3f, 0.9f, 0.3f));
        _lblEffect.style.whiteSpace = WhiteSpace.Normal;
        _lblEffect.style.marginTop = 6;
        
        _lblUpgradeInfo = CreateTooltipLabel(11, new Color(0.9f, 0.6f, 0.2f));
        _lblUpgradeInfo.style.whiteSpace = WhiteSpace.Normal;
        _lblUpgradeInfo.style.marginTop = 6;

        _lblGuide = CreateTooltipLabel(11, new Color(0.5f, 0.5f, 0.5f));
        _lblGuide.style.marginTop = 10;
        _lblGuide.text = "💡 좌클릭: 상호작용 | 우클릭: 업그레이드";

        // 컨테이너 순서대로 등록 (기존 이름 자리에 타이틀바 장착)
        _tooltipContainer.Add(_titleBarContainer); 
        _tooltipContainer.Add(_lblTargetLevel);
        _tooltipContainer.Add(_lblDescription); 
        _tooltipContainer.Add(_lblEffect);
        _tooltipContainer.Add(_lblUpgradeInfo); 
        _tooltipContainer.Add(_lblGuide);

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

        var curLevelData = data.GetLevelData(curLevel);

        _lblTargetName.text = data.DisplayName;
        _lblTargetLevel.text = $"레벨: {curLevel} / {data.levels.Count}";
        _lblDescription.text = string.IsNullOrEmpty(data.Description) ? "마을의 주요 건물입니다." : data.Description;

        // =========================================================================
        // 📊 [3순위 우측 상단 횟수 가이드라인 셋업]
        // =========================================================================
        // 컴포넌트 구조에서 횟수 부품(Counter) 정보를 가져옵니다.
        int currentUses = (building.Counter != null) ? building.Counter.CurrentTurnUses : 0;
        int maxUses = curLevelData.maxUses;

        if (maxUses > 0)
        {
            // 남은 횟수 산출 (최대 횟수 - 현재 사용 횟수)
            int remainingUses = maxUses - currentUses;
            if (remainingUses < 0) remainingUses = 0;

            _lblUseCount.text = $"이용 가능: {remainingUses} / {maxUses}회";
            _lblUseCount.style.display = DisplayStyle.Flex; // 표출 활성화
        }
        else
        {
            // 대장간이나 창고처럼 maxUses가 0 이하인 무제한 건물은 우측 상단 가이드 텍스트 숨김 처리
            _lblUseCount.style.display = DisplayStyle.None;
        }

        // 1레벨부터 현재 레벨까지 고유 수치 누적 합산 계산
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
                totalEffectValue = levelData.effectValue;
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

        // 다음 레벨 수치 가이드라인 매핑
        int nextLevel = curLevel + 1;
        if (nextLevel <= data.levels.Count)
        {
            var nextLevelData = data.GetLevelData(nextLevel);

            int nextMaterialCap = totalMaterialCap + nextLevelData.materialCapAdd;
            int nextFrontierCap = totalFrontierCap + nextLevelData.frontierCapAdd;
            int nextSlotExpansion = totalSlotExpansion + nextLevelData.slotExpansion;
            int nextEffectValue = nextLevelData.effectValue;

            string upText = "[업그레이드 시 최종 변경값]\n";
            bool hasChanges = false;

            if (nextLevelData.materialCapAdd > 0) { upText += $" 자원 보관 한도: {totalMaterialCap} ➔ {nextMaterialCap} 증가\n"; hasChanges = true; }
            if (nextLevelData.frontierCapAdd > 0) { upText += $" 개척 가능 한도: {totalFrontierCap} ➔ {nextFrontierCap} 증가\n"; hasChanges = true; }
            if (nextLevelData.slotExpansion > 0) { upText += $" 인벤토리 슬롯: {totalSlotExpansion}칸 ➔ {nextSlotExpansion}칸 증가\n"; hasChanges = true; }
            if (nextLevelData.effectValue > 0) { upText += $" 효과 고유 수치: {totalEffectValue} ➔ {nextEffectValue} 증가\n"; hasChanges = true; }
            if (data.buildingType == BuildingType.Smithy || data.name.Contains("Smithy")) { upText += $" 장비 최고 강화 한도: {curLevel}단계 ➔ {nextLevel}단계 제한 확장\n"; hasChanges = true; }

            // =========================================================================
            // 📊 [3순위 다음 레벨 이용 제한 변동폭 가이드 주입 - 비누적 상태형 매핑 규칙]
            // =========================================================================
            if (maxUses > 0 && nextLevelData.maxUses != maxUses)
            {
                // 리더님 요청 반영: 합산 방식이 아닌 테이블 고유 단발성 수치 그대로 1대1 매칭
                upText += $" 이용 제한 횟수: {maxUses}회 ➔ {nextLevelData.maxUses}회로 제한 확장\n";
                hasChanges = true;
            }

            if (!hasChanges) upText += $" 건물의 전반적인 기능성 및 내구도 증가\n";
            _lblUpgradeInfo.text = upText;
            _lblUpgradeInfo.style.display = DisplayStyle.Flex;
        }
        else
        {
            _lblUpgradeInfo.text = "최고 레벨에 도달하여 추가 업그레이드가 불가능합니다.";
            _lblUpgradeInfo.style.display = DisplayStyle.Flex;
        }

        // =========================================================================
        // 🖥️ [스크린 이탈 방지] 화면 밖으로 탈출하지 않도록 가두는 경해선 Clamping 연산
        // =========================================================================
        float tooltipWidth = 300f;  // 안전 마진을 고려한 툴팁 예측 가로 크기
        float tooltipHeight = 220f; // 안전 마진을 고려한 툴팁 예측 세로 크기

        // 1. 가로축(X) 검사: 마우스 우측에 띄웠을 때 화면 우측 벽을 뚫고 나간다면?
        float finalX = mousePosition.x + 15f;
        if (finalX + tooltipWidth > Screen.width)
        {
            // 마우스 왼쪽 공간으로 툴팁을 반전 배치하여 탈출 방지
            finalX = mousePosition.x - tooltipWidth - 15f;
        }

        // 2. 세로축(Y) 검사: 마우스 하단에 띄웠을 때 화면 바닥 벽을 뚫고 나간다면?
        // UI Toolkit 기준 Y는 상단이 0이므로, 값이 커질수록 화면 아래로 내려갑니다.
        float finalY = mousePosition.y + 15f;
        if (finalY + tooltipHeight > Screen.height)
        {
            // 마우스 위쪽 공간으로 툴팁을 반전 배치하여 탈출 방지
            finalY = mousePosition.y - tooltipHeight - 15f;
        }

        // 3. 화면 최소값 안전장치 (좌측 벽이나 상단 벽을 뚫고 나가는 음수 값 차단)
        if (finalX < 5f) finalX = 5f;
        if (finalY < 5f) finalY = 5f;

        // 최종 계산된 무결성 좌표로 툴팁 배치 및 출력
        _tooltipContainer.style.left = new StyleLength(new Length(finalX, LengthUnit.Pixel));
        _tooltipContainer.style.top = new StyleLength(new Length(finalY, LengthUnit.Pixel));
        _tooltipContainer.style.visibility = Visibility.Visible;
        _tooltipContainer.BringToFront();
    }

    public void HideTooltip()
    {
        if (_tooltipContainer != null)
            _tooltipContainer.style.visibility = Visibility.Hidden;
    }
}