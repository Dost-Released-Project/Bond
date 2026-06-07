using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace BattleSystem.UI
{
    public class SkillTooltipView : MonoBehaviour
    {
        private VisualElement _root;
        private VisualElement _tooltipContainer;

        private Label _lblSkillName;
        private Label _lblSkillType;
        private Label _lblTargetInfo;
        private Label _lblSkillDetails;
        private Label _lblSkillDesc;

        private void OnEnable()
        {
            var uiDoc = GetComponent<UIDocument>();
            if (uiDoc == null) return;
            
            _root = uiDoc.rootVisualElement;

            _tooltipContainer = new VisualElement();
            _tooltipContainer.style.position = Position.Absolute;
            _tooltipContainer.style.visibility = Visibility.Hidden;
            _tooltipContainer.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);

            // 테두리 스타일
            _tooltipContainer.style.borderLeftWidth = 1; _tooltipContainer.style.borderRightWidth = 1;
            _tooltipContainer.style.borderTopWidth = 1; _tooltipContainer.style.borderBottomWidth = 1;
            _tooltipContainer.style.borderLeftColor = new Color(0.4f, 0.6f, 0.8f);
            _tooltipContainer.style.borderRightColor = new Color(0.4f, 0.6f, 0.8f);
            _tooltipContainer.style.borderTopColor = new Color(0.4f, 0.6f, 0.8f);
            _tooltipContainer.style.borderBottomColor = new Color(0.4f, 0.6f, 0.8f);

            _tooltipContainer.style.borderTopLeftRadius = 6; _tooltipContainer.style.borderTopRightRadius = 6;
            _tooltipContainer.style.borderBottomLeftRadius = 6; _tooltipContainer.style.borderBottomRightRadius = 6;

            // 패딩 및 크기 설정
            _tooltipContainer.style.paddingLeft = 14; _tooltipContainer.style.paddingRight = 14;
            _tooltipContainer.style.paddingTop = 14; _tooltipContainer.style.paddingBottom = 14;

            _tooltipContainer.style.width = StyleKeyword.Auto;
            _tooltipContainer.style.height = StyleKeyword.Auto;
            _tooltipContainer.style.minWidth = 250;
            _tooltipContainer.style.maxWidth = 350;
            _tooltipContainer.pickingMode = PickingMode.Ignore;

            // 1. 스킬 명칭 (Title)
            _lblSkillName = new Label();
            _lblSkillName.style.fontSize = 20;
            _lblSkillName.style.unityFontStyleAndWeight = FontStyle.Bold;
            _lblSkillName.style.color = new Color(1f, 0.85f, 0.4f); // 골드 색상
            _lblSkillName.style.whiteSpace = WhiteSpace.Normal;
            _lblSkillName.style.marginBottom = 5;

            // 2. 스킬 타입 & 타겟 정보 컨테이너
            var typeTargetContainer = new VisualElement();
            typeTargetContainer.style.flexDirection = FlexDirection.Row;
            typeTargetContainer.style.justifyContent = Justify.SpaceBetween;
            typeTargetContainer.style.marginBottom = 10;
            typeTargetContainer.style.paddingBottom = 5;
            typeTargetContainer.style.borderBottomWidth = 1;
            typeTargetContainer.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            _lblSkillType = new Label();
            _lblSkillType.style.fontSize = 14;
            _lblSkillType.style.color = new Color(0.8f, 0.8f, 0.8f);
            _lblSkillType.style.unityFontStyleAndWeight = FontStyle.Bold;

            _lblTargetInfo = new Label();
            _lblTargetInfo.style.fontSize = 14;
            _lblTargetInfo.style.color = new Color(0.6f, 0.8f, 1f);
            
            typeTargetContainer.Add(_lblSkillType);
            typeTargetContainer.Add(_lblTargetInfo);

            // 3. 스킬 상세 수치 (데미지, 쿨타임, 지속시간 등)
            _lblSkillDetails = new Label();
            _lblSkillDetails.style.fontSize = 14;
            _lblSkillDetails.style.color = Color.white;
            _lblSkillDetails.style.whiteSpace = WhiteSpace.Normal;
            _lblSkillDetails.style.marginBottom = 10;

            // 4. 스킬 설명 (Lore / Description)
            _lblSkillDesc = new Label();
            _lblSkillDesc.style.fontSize = 13;
            _lblSkillDesc.style.color = new Color(0.7f, 0.7f, 0.7f);
            _lblSkillDesc.style.whiteSpace = WhiteSpace.Normal;

            // 컨테이너 조립
            _tooltipContainer.Add(_lblSkillName);
            _tooltipContainer.Add(typeTargetContainer);
            _tooltipContainer.Add(_lblSkillDetails);
            _tooltipContainer.Add(_lblSkillDesc);

            _root.Add(_tooltipContainer);
        }

        private void OnDisable()
        {
            if (_root != null && _tooltipContainer != null && _root.Contains(_tooltipContainer))
            {
                _root.Remove(_tooltipContainer);
            }
        }

        public void ShowTooltip(SkillBase skill, Vector2 mousePosition)
        {
            if (skill == null || skill.Data == null) return;

            var data = skill.Data;

            // 1. 이름 세팅
            _lblSkillName.text = data.DisplayName ?? data.name;

            // 2. 타입 및 타겟 세팅
            _lblSkillType.text = $"[{data.Type}]";
            _lblTargetInfo.text = $"타겟: {data.TargetingType} / {data.Target}";

            // 타입별 색상 조정
            switch (data.Type)
            {
                case SkillType.OFFENSIVE: _lblSkillType.style.color = new Color(1f, 0.4f, 0.4f); break;
                case SkillType.SPELL: _lblSkillType.style.color = new Color(0.8f, 0.4f, 1f); break;
                case SkillType.SUPPORT: _lblSkillType.style.color = new Color(0.4f, 1f, 0.4f); break;
                case SkillType.DEFENSIVE: _lblSkillType.style.color = new Color(0.4f, 0.8f, 1f); break;
                default: _lblSkillType.style.color = new Color(0.8f, 0.8f, 0.8f); break;
            }

            // 3. 상세 수치 세팅
            string details = "";
            if (data.Value > 0)
            {
                string valueLabel = data.Type == SkillType.SUPPORT ? "회복/버프량" : "효과 수치 (데미지)";
                details += $"• {valueLabel}: {data.Value}\n";
            }
            if (data.CoolTime > 0) details += $"• 재사용 대기: {data.CoolTime} 턴\n";
            if (data.Duration > 0) details += $"• 지속 시간: {data.Duration} 턴\n";

            _lblSkillDetails.text = details.TrimEnd('\n');
            _lblSkillDetails.style.display = string.IsNullOrEmpty(details) ? DisplayStyle.None : DisplayStyle.Flex;

            // 4. 설명 텍스트 세팅
            _lblSkillDesc.text = data.Description;

            // 5. 화면 이탈 방지 로직 (Clamping)
            float tooltipWidth = 280f;  // 안전 마진 툴팁 가로
            float tooltipHeight = 150f; // 안전 마진 툴팁 세로

            float finalX = mousePosition.x + 15f;
            if (finalX + tooltipWidth > Screen.width)
            {
                finalX = mousePosition.x - tooltipWidth - 15f;
            }

            float finalY = mousePosition.y + 15f;
            if (finalY + tooltipHeight > Screen.height)
            {
                finalY = mousePosition.y - tooltipHeight - 15f;
            }

            if (finalX < 5f) finalX = 5f;
            if (finalY < 5f) finalY = 5f;

            _tooltipContainer.style.left = new StyleLength(new Length(finalX, LengthUnit.Pixel));
            _tooltipContainer.style.top = new StyleLength(new Length(finalY, LengthUnit.Pixel));
            
            _tooltipContainer.style.visibility = Visibility.Visible;
            _tooltipContainer.BringToFront();
        }

        public void HideTooltip()
        {
            if (_tooltipContainer != null)
            {
                _tooltipContainer.style.visibility = Visibility.Hidden;
            }
        }
    }
}