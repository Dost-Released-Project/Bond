using UnityEngine.UIElements;

namespace Bond.UI.Town
{
    public class CharacterDetailPresenter
    {
        private readonly VisualElement _panel;
        private readonly Label _titleLabel;
        private readonly CharacterSelector _selector;

        public CharacterDetailPresenter(VisualElement root, CharacterSelector selector)
        {
            _selector = selector;

            _panel      = root.Q("character-detail");
            _titleLabel = root.Q<Label>("character-detail__title");

            root.Q<Button>("character-detail__close-btn").clicked += OnCloseBtnClicked;

            _selector.OnSelectionChanged += OnSelectionChanged;
        }

        public void Show(BaseCharacter character)
        {
            _titleLabel.text = character.Name;
            _panel.RemoveFromClassList("character-detail--hidden");
            _panel.AddToClassList("character-detail--visible");
        }

        public void Hide()
        {
            _panel.RemoveFromClassList("character-detail--visible");
            _panel.AddToClassList("character-detail--hidden");
        }

        private void OnSelectionChanged(BaseCharacter character)
        {
            if (character == null)
                Hide();
            else
                Show(character);
        }

        private void OnCloseBtnClicked()
        {
            _selector.Deselect();
        }
    }
}