using Bond.Embark;
using Bond.UI;
using Bond.UI.Town;
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
        private CharacterDetailPresenter _characterDetail;
        private AccessoryBagView _accessoryBagView;
        private ITotalInventory _townInventory;

        private TownRosterPanelPresenter _rosterPresenter;
        private EmbarkPresenter _embarkPresenter;
        private Button _toggleBtn;

        [Inject]
        public void Construct(
            CharacterSelector selector,
            Roster roster,
            EmbarkController embarkController,
            CharacterDetailPresenter characterDetail,
            AccessoryBagView accessoryBagView,
            ITotalInventory townInventory)
        {
            _selector       = selector;
            _roster         = roster;
            _embarkController = embarkController;
            _characterDetail  = characterDetail;
            _accessoryBagView = accessoryBagView;
            _townInventory    = townInventory;
        }

        private void Start()
        {
            var root = townUIDocument.rootVisualElement;

            _rosterPresenter = new TownRosterPanelPresenter(root, _roster, _selector);
            _embarkPresenter = new EmbarkPresenter(root, _embarkController, _roster);

            foreach (var tc in root.Query<TemplateContainer>().ToList())
                tc.pickingMode = PickingMode.Ignore;

            _toggleBtn = root.Q<Button>("roster-toggle-btn");
            _toggleBtn.clicked += ToggleRoster;
            root.Q<Button>("embark-btn").clicked += _embarkController.Open;

            _characterDetail.OnCloseRequested       += _selector.Deselect;
            _characterDetail.OnInventoryOpenRequested += _accessoryBagView.ToggleWindow;

            _selector.OnSelectionChanged += character =>
            {
                if (character != null)
                    _characterDetail.Show(character, CharacterDetailViewMode.FullEdit, _townInventory);
                else
                    _characterDetail.Hide();
            };
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
