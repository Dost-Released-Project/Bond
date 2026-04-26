using System;
using Bond.Embark;
using UnityEngine.UIElements;

namespace Bond.UI.RoleReactionEditor
{
    public class CharacterInfoPresenter
    {
        public event Action<RoleType> OnRoleChanged;

        private readonly Label _nameLabel;
        private readonly Label _classLabel;
        private readonly Label _statStrLabel;
        private readonly Label _statAgiLabel;
        private readonly Label _statIntLabel;
        private readonly Label _statStressLabel;
        private readonly VisualElement _traitContainer;
        private readonly Button _btnTanker;
        private readonly Button _btnDealer;
        private readonly Button _btnSupporter;

        private BaseCharacter _current;

        public CharacterInfoPresenter(VisualElement root)
        {
            _nameLabel       = root.Q<Label>("charNameLabel");
            _classLabel      = root.Q<Label>("charClassLabel");
            _statStrLabel    = root.Q<Label>("statStrLabel");
            _statAgiLabel    = root.Q<Label>("statAgiLabel");
            _statIntLabel    = root.Q<Label>("statIntLabel");
            _statStressLabel = root.Q<Label>("statStressLabel");
            _traitContainer  = root.Q<VisualElement>("traitTagContainer");

            _btnTanker    = root.Q<Button>("roleBtnTanker");
            _btnDealer    = root.Q<Button>("roleBtnDealer");
            _btnSupporter = root.Q<Button>("roleBtnSupporter");

            _btnTanker.clicked    += () => ApplyRole(RoleType.Tanker);
            _btnDealer.clicked    += () => ApplyRole(RoleType.Dealer);
            _btnSupporter.clicked += () => ApplyRole(RoleType.Supporter);
        }

        public void Bind(BaseCharacter character)
        {
            _current = character;

            _nameLabel.text = character.UnitName;

            var stat = character.StatComponent;
            _classLabel.text      = stat != null ? stat.ClassType.ToString() : "—";
            _statStrLabel.text    = stat != null ? stat.STR.ToString()       : "—";
            _statAgiLabel.text    = stat != null ? stat.AGI.ToString()       : "—";
            _statIntLabel.text    = stat != null ? stat.INT.ToString()       : "—";
            _statStressLabel.text = stat != null ? stat.insanity.ToString()  : "—";

            RefreshTraits();
            RefreshRoleButtons(character.roleType);
        }

        private void RefreshTraits()
        {
            _traitContainer.Clear();
            if (_current == null) return;

            foreach (var trait in _current.traits)
            {
                if (trait == null || string.IsNullOrEmpty(trait.Name)) continue;

                var tag = new VisualElement();
                tag.AddToClassList("rre-trait-tag");

                var lbl = new Label(trait.Name);
                lbl.AddToClassList("rre-trait-tag__label");
                tag.Add(lbl);

                _traitContainer.Add(tag);
            }
        }

        private void ApplyRole(RoleType role)
        {
            if (_current == null) return;
            _current.roleType = role;
            RefreshRoleButtons(role);
            OnRoleChanged?.Invoke(role);
        }

        private void RefreshRoleButtons(RoleType current)
        {
            SetSelected(_btnTanker,    current == RoleType.Tanker);
            SetSelected(_btnDealer,    current == RoleType.Dealer);
            SetSelected(_btnSupporter, current == RoleType.Supporter);
        }

        private static void SetSelected(Button btn, bool selected)
        {
            if (selected) btn.AddToClassList("rre-role-btn--selected");
            else          btn.RemoveFromClassList("rre-role-btn--selected");
        }
    }
}