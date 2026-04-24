using System;
using System.Collections.Generic;
using Reactions;
using UnityEngine.UIElements;

namespace Bond.UI.RoleReactionEditor
{
    public class SkillSelectPanelPresenter
    {
        public event Action<SkillBase> OnSkillSelected;

        private VisualElement _currentGrid;

        public void Show(VisualElement grid, SkillBase[] skills, SkillBase currentSelected)
        {
            _currentGrid = grid;
            grid.Clear();

            foreach (var skill in skills)
            {
                if (skill?.Data == null) continue;

                var card = BuildCard(skill, skill == currentSelected);
                var captured = skill;
                card.clicked += () =>
                {
                    RefreshSelection(captured);
                    OnSkillSelected?.Invoke(captured);
                };
                grid.Add(card);
            }
        }

        private void RefreshSelection(SkillBase selected)
        {
            if (_currentGrid == null) return;
            foreach (var child in _currentGrid.Children())
            {
                child.RemoveFromClassList("rre-skill-card--selected");
                if (child.userData is SkillBase s && s == selected)
                    child.AddToClassList("rre-skill-card--selected");
            }
        }

        private static Button BuildCard(SkillBase skill, bool isSelected)
        {
            var card = new Button();
            card.AddToClassList("rre-skill-card");
            card.userData = skill;

            var icon = new VisualElement();
            icon.AddToClassList("rre-skill-card__icon");

            var name = new Label(skill.Data.DisplayName);
            name.AddToClassList("rre-skill-card__name");

            var desc = new Label(skill.Data.Description);
            desc.AddToClassList("rre-skill-card__desc");

            card.Add(icon);
            card.Add(name);
            card.Add(desc);

            if (isSelected) card.AddToClassList("rre-skill-card--selected");
            return card;
        }
    }
}