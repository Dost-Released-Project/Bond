using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Bond.UI.Town
{
    public class TownUIController : MonoBehaviour
    {
        [SerializeField] private UIDocument townUIDocument;

        private CharacterSelector _selector;
        private Roster _roster;
        private TownRosterPanelPresenter _rosterPresenter;
        private CharacterDetailPresenter _detailPresenter;
        private Button _toggleBtn;

        [Inject]
        public void Construct(CharacterSelector selector, Roster roster)
        {
            _selector = selector;
            _roster   = roster;
        }

        private void Start()
        {
            var root = townUIDocument.rootVisualElement;

            _rosterPresenter = new TownRosterPanelPresenter(root, _roster, _selector);
            _detailPresenter = new CharacterDetailPresenter(root, _selector);

            foreach (var tc in root.Query<TemplateContainer>().ToList())
                tc.pickingMode = PickingMode.Ignore;

            _toggleBtn = root.Q<Button>("roster-toggle-btn");
            _toggleBtn.clicked += ToggleRoster;
        }

        private void ToggleRoster()
        {
            if (_rosterPresenter.IsVisible)
            {
                _rosterPresenter.Hide();
                _toggleBtn.text = "대원 명부 ▶";
                _toggleBtn.RemoveFromClassList("roster-toggle-btn--active");
            }
            else
            {
                _rosterPresenter.Show();
                _toggleBtn.text = "대원 명부 ◀";
                _toggleBtn.AddToClassList("roster-toggle-btn--active");
            }
        }
    }
}