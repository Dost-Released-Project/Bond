using UnityEngine;
using UnityEngine.UIElements;

namespace BattleSystem.UI
{
    /// <summary>
    /// 스킬 툴팁의 리치 콘텐츠(제목·타입/타겟·수치·설명)를 만든다. 배치·표시·경계 clamp는 TooltipPopup이 담당.
    /// 기존 SkillTooltipView의 룩(인라인 색/레이아웃)을 그대로 보존한다 — 툴팁 스타일 통일은 별도 논의 대상.
    /// </summary>
    public static class SkillTooltipContent
    {
        public static VisualElement Build(SkillBase skill)
        {
            if (skill?.Data == null) return null;
            var data = skill.Data;

            var container = new VisualElement();
            container.style.backgroundColor = new Color(0.12f, 0.12f, 0.12f, 0.95f);

            var border = new Color(0.4f, 0.6f, 0.8f);
            container.style.borderLeftWidth = 1; container.style.borderRightWidth = 1;
            container.style.borderTopWidth = 1; container.style.borderBottomWidth = 1;
            container.style.borderLeftColor = border; container.style.borderRightColor = border;
            container.style.borderTopColor = border; container.style.borderBottomColor = border;
            container.style.borderTopLeftRadius = 6; container.style.borderTopRightRadius = 6;
            container.style.borderBottomLeftRadius = 6; container.style.borderBottomRightRadius = 6;
            container.style.paddingLeft = 14; container.style.paddingRight = 14;
            container.style.paddingTop = 14; container.style.paddingBottom = 14;
            container.style.minWidth = 250; container.style.maxWidth = 350;

            // 1. 이름
            var name = new Label(data.DisplayName ?? data.name);
            name.style.fontSize = 20;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            name.style.color = new Color(1f, 0.85f, 0.4f);
            name.style.whiteSpace = WhiteSpace.Normal;
            name.style.marginBottom = 5;
            container.Add(name);

            // 2. 타입 / 타겟
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.justifyContent = Justify.SpaceBetween;
            row.style.marginBottom = 10;
            row.style.paddingBottom = 5;
            row.style.borderBottomWidth = 1;
            row.style.borderBottomColor = new Color(0.3f, 0.3f, 0.3f);

            var type = new Label($"[{data.Type}]");
            type.style.fontSize = 14;
            type.style.unityFontStyleAndWeight = FontStyle.Bold;
            type.style.color = TypeColor(data.Type);

            var target = new Label($"타겟: {data.TargetingType} / {data.Target}");
            target.style.fontSize = 14;
            target.style.color = new Color(0.6f, 0.8f, 1f);

            row.Add(type);
            row.Add(target);
            container.Add(row);

            // 3. 상세 수치
            string details = "";
            if (data.Value > 0)
            {
                string valueLabel = data.Type == SkillType.SUPPORT ? "회복/버프량" : "효과 수치 (데미지)";
                details += $"• {valueLabel}: {data.Value}\n";
            }
            if (data.CoolTime > 0) details += $"• 재사용 대기: {data.CoolTime} 턴\n";
            if (data.Duration > 0) details += $"• 지속 시간: {data.Duration} 턴\n";

            if (!string.IsNullOrEmpty(details))
            {
                var detail = new Label(details.TrimEnd('\n'));
                detail.style.fontSize = 14;
                detail.style.color = Color.white;
                detail.style.whiteSpace = WhiteSpace.Normal;
                detail.style.marginBottom = 10;
                container.Add(detail);
            }

            // 4. 설명
            if (!string.IsNullOrEmpty(data.Description))
            {
                var desc = new Label(data.Description);
                desc.style.fontSize = 13;
                desc.style.color = new Color(0.7f, 0.7f, 0.7f);
                desc.style.whiteSpace = WhiteSpace.Normal;
                container.Add(desc);
            }

            return container;
        }

        private static Color TypeColor(SkillType type) => type switch
        {
            SkillType.OFFENSIVE => new Color(1f, 0.4f, 0.4f),
            SkillType.SPELL     => new Color(0.8f, 0.4f, 1f),
            SkillType.SUPPORT   => new Color(0.4f, 1f, 0.4f),
            SkillType.DEFENSIVE => new Color(0.4f, 0.8f, 1f),
            _                   => new Color(0.8f, 0.8f, 0.8f),
        };
    }
}
