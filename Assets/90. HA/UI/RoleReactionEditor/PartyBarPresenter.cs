using System;
using System.Collections.Generic;
using Bond.PartyManagement;
using UnityEngine.UIElements;

namespace Bond.UI.RoleReactionEditor
{
    public class PartyBarPresenter
    {
        public event Action<int> OnCharacterSelected;

        private readonly VisualElement _iconContainer;
        private readonly Label _summaryLabel;
        private readonly VisualTreeAsset _iconTemplate;
        private readonly List<Button> _iconButtons = new();

        public PartyBarPresenter(VisualElement root, VisualTreeAsset iconTemplate)
        {
            _iconContainer = root.Q<VisualElement>("partyIconContainer");
            _summaryLabel  = root.Q<Label>("roleSummaryLabel");
            _iconTemplate  = iconTemplate;
        }

        public void Bind(IReadOnlyList<BaseCharacter> party)
        {
            _iconContainer.Clear();
            _iconButtons.Clear();

            for (int i = 0; i < party.Count; i++)
            {
                int capturedIndex = i;
                var character = party[i];

                var tree = _iconTemplate.CloneTree();
                var btn  = tree.Q<Button>();
                tree.Q<Label>("iconName").text = character.UnitName;
                btn.clicked += () => OnCharacterSelected?.Invoke(capturedIndex);

                _iconContainer.Add(tree);
                _iconButtons.Add(btn);
            }

            RefreshSummary(party);
        }

        public void SetSelected(int index)
        {
            for (int i = 0; i < _iconButtons.Count; i++)
            {
                if (i == index) _iconButtons[i].AddToClassList("rre-char-icon--selected");
                else            _iconButtons[i].RemoveFromClassList("rre-char-icon--selected");
            }
        }

        public void RefreshSummary(IReadOnlyList<BaseCharacter> party)
        {
            int tanker = 0, dealer = 0, supporter = 0;
            foreach (var c in party)
            {
                switch (c.roleType)
                {
                    case RoleType.Tanker:    tanker++;    break;
                    case RoleType.Dealer:    dealer++;    break;
                    case RoleType.Supporter: supporter++; break;
                }
            }
            _summaryLabel.text = $"탱커 {tanker} / 딜러 {dealer} / 서포터 {supporter}";
        }
    }
}