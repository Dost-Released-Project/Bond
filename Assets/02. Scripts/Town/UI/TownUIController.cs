using Bond.Embark;
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
        private EmbarkController _embarkController;
        private TownRosterPanelPresenter _rosterPresenter;
        private CharacterDetailPresenter _detailPresenter;
        private EmbarkPresenter _embarkPresenter;
        private Button _toggleBtn;

        [Inject]
        public void Construct(CharacterSelector selector, Roster roster, EmbarkController embarkController)
        {
            _selector = selector;
            _roster   = roster;
            _embarkController = embarkController;
        }

        private void Start()
        {
            var root = townUIDocument.rootVisualElement;

            _rosterPresenter = new TownRosterPanelPresenter(root, _roster, _selector);
            _detailPresenter = new CharacterDetailPresenter(root, _selector);
            _embarkPresenter = new EmbarkPresenter(root, _embarkController, _roster);

            foreach (var tc in root.Query<TemplateContainer>().ToList())
                tc.pickingMode = PickingMode.Ignore;

            _toggleBtn = root.Q<Button>("roster-toggle-btn");
            _toggleBtn.clicked += ToggleRoster;

            root.Q<Button>("embark-btn").clicked += _embarkController.Open;
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