using System.Collections.Generic;
using Bond.Embark;
using Bond.Expedition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Bond.UI.Town
{
    public class EmbarkPresenter
    {
        private readonly VisualElement _overlay;
        private readonly EmbarkController _controller;
        private readonly TownRosterPanelPresenter _embarkRosterPresenter;

        private readonly VisualElement _step1Panel;
        private readonly VisualElement _step2Panel;
        private readonly VisualElement _footer1;
        private readonly VisualElement _footer2;
        private readonly Label _partyCountLabel;
        private readonly VisualElement[] _partySlots = new VisualElement[4];
        private readonly VisualElement _townInventoryList;
        private readonly VisualElement _raidSuppliesList;
        private readonly Label _destNameLabel;
        private readonly Label _destMetaLabel;
        private readonly VisualElement _regionList;
        private readonly Label _step2Hint;
        private readonly string _defaultStep2Hint;
        private readonly Dictionary<string, VisualElement> _regionCards = new();

        private readonly List<BaseCharacter> _partySnapshot = new(4);

        public EmbarkPresenter(VisualElement townRoot, EmbarkController controller, Roster roster, CharacterDetailPresenter characterDetail, ITotalInventory townInventory)
        {
            _controller = controller;

            _overlay = townRoot.Q("embark-overlay");

            var embarkSelector = new CharacterSelector();
            _embarkRosterPresenter = new TownRosterPanelPresenter(_overlay, roster, embarkSelector);

            _step1Panel = _overlay.Q("embark__step1");
            _step2Panel = _overlay.Q("embark__step2");
            _footer1 = _overlay.Q("embark__footer--step1");
            _footer2 = _overlay.Q("embark__footer--step2");
            _partyCountLabel = _overlay.Q<Label>("embark__party-count");

            _destNameLabel = _overlay.Q<Label>("dest-info__name");
            _destMetaLabel = _overlay.Q<Label>("dest-info__meta");

            for (int i = 0; i < 4; i++)
            {
                _partySlots[i] = _overlay.Q($"embark__party-slot-{i}");
                int slotIdx = i;
                _partySlots[i].RegisterCallback<ClickEvent>(_ => OnPartySlotClicked(slotIdx));
            }

            _townInventoryList = _overlay.Q("embark__town-inventory__list");
            _raidSuppliesList = _overlay.Q("embark__raid-supplies__list");

            _overlay.Q<Button>("tab-btn--step1").clicked += () => GoToStep(1);
            _overlay.Q<Button>("tab-btn--step2").clicked += () => GoToStep(2);
            _overlay.Q<Button>("btn-cancel").clicked += _controller.Close;
            _overlay.Q<Button>("btn-next").clicked += () => GoToStep(2);
            _overlay.Q<Button>("btn-prev").clicked += () => GoToStep(1);
            _overlay.Q<Button>("btn-depart").clicked += _controller.ConfirmEmbark;

            _regionList = _overlay.Q("embark__region-list");
            _step2Hint = _footer2.Q<Label>(className: "embark__footer-hint");
            _defaultStep2Hint = _step2Hint != null ? _step2Hint.text : "";
            BuildRegionList();

            _embarkRosterPresenter.OnCardClicked = character =>
            {
                _controller.TogglePartyMember(character);
            };
            _embarkRosterPresenter.OnCardRightClicked += (character) =>
            {
                characterDetail.Show(character, CharacterDetailEditMode.FullEdit, townInventory);
            };

            _controller.OnOverlayOpened += Show;
            _controller.OnOverlayClosed += Hide;
            _controller.OnDataChanged += OnDataChanged;
            _controller.OnRegionChanged += RefreshSelectedRegion;
            _controller.OnEmbarkBlocked += ShowEmbarkWarning;
        }

        public void Show()
        {
            _overlay.RemoveFromClassList("embark-overlay--hidden");
            _overlay.AddToClassList("embark-overlay--visible");
            _embarkRosterPresenter.Show();
            GoToStep(1);
            RefreshSelectedRegion(_controller.SelectedRegion);
        }

        public void Hide()
        {
            _overlay.RemoveFromClassList("embark-overlay--visible");
            _overlay.AddToClassList("embark-overlay--hidden");
            _embarkRosterPresenter.Hide();
        }

        public void GoToStep(int step)
        {
            bool isStep1 = step == 1;
            _step1Panel.style.display = isStep1 ? DisplayStyle.Flex : DisplayStyle.None;
            _step2Panel.style.display = isStep1 ? DisplayStyle.None : DisplayStyle.Flex;
            _footer1.style.display = isStep1 ? DisplayStyle.Flex : DisplayStyle.None;
            _footer2.style.display = isStep1 ? DisplayStyle.None : DisplayStyle.Flex;

            _overlay.Q<Button>("tab-btn--step1").EnableInClassList("embark__tab-btn--active", isStep1);
            _overlay.Q<Button>("tab-btn--step2").EnableInClassList("embark__tab-btn--active", !isStep1);
        }

        public void RefreshPartySlots(List<BaseCharacter> party)
        {
            _partySnapshot.Clear();
            _partySnapshot.AddRange(party);

            for (int i = 0; i < 4; i++)
            {
                var slot = _partySlots[i];
                slot.Clear();

                if (i < party.Count)
                {
                    var character = party[i];
                    slot.RemoveFromClassList("embark__party-slot--empty");
                    slot.AddToClassList("embark__party-slot--filled");

                    var avatar = new VisualElement();
                    avatar.AddToClassList("embark__slot-avatar");
                    if (!string.IsNullOrEmpty(character.ImageAddress))
                        LoadAvatarAsync(character.ImageAddress, avatar).Forget();
                    else
                    {
                        var initial = new Label(character.Name.Length > 0 ? character.Name[0].ToString() : "?");
                        initial.AddToClassList("embark__slot-initial");
                        avatar.Add(initial);
                    }

                    var nameLabel = new Label(character.Name);
                    nameLabel.AddToClassList("embark__slot-name");

                    var classLabel = new Label(character.Profession?.Name ?? "");
                    classLabel.AddToClassList("embark__slot-class");

                    slot.Add(avatar);
                    slot.Add(nameLabel);
                    slot.Add(classLabel);
                }
                else
                {
                    slot.RemoveFromClassList("embark__party-slot--filled");
                    slot.AddToClassList("embark__party-slot--empty");

                    var emptyLabel = new Label("— 빈 슬롯 —");
                    emptyLabel.AddToClassList("embark__slot-empty-label");
                    slot.Add(emptyLabel);
                }
            }

            _embarkRosterPresenter.UpdatePartyHighlight(party);
        }

        public void RefreshInventory(List<InventorySlot> townSlots, List<InventorySlot> raidSlots)
        {
            _townInventoryList.Clear();
            for (int i = 0; i < townSlots.Count; i++)
            {
                if (townSlots[i].IsEmpty) continue;
                _townInventoryList.Add(BuildInventoryRow(i, townSlots[i], isTown: true));
            }

            _raidSuppliesList.Clear();
            for (int i = 0; i < raidSlots.Count; i++)
            {
                if (raidSlots[i].IsEmpty) continue;
                _raidSuppliesList.Add(BuildInventoryRow(i, raidSlots[i], isTown: false));
            }
        }

        public void SetDestinationInfo(string name, string meta)
        {
            if (_destNameLabel != null) _destNameLabel.text = name;
            if (_destMetaLabel != null) _destMetaLabel.text = meta;
        }

        public void UpdatePartyCount(int current, int max)
        {
            if (_partyCountLabel != null)
                _partyCountLabel.text = $"인원 {current} / {max}";
        }

        private void OnDataChanged(EmbarkData data)
        {
            RefreshPartySlots(data.SelectedParty);
            RefreshInventory(data.TownInventorySlots, data.PreparedSupplies);
            UpdatePartyCount(data.SelectedParty.Count, 4);
        }

        private void OnPartySlotClicked(int slotIdx)
        {
            if (slotIdx < _partySnapshot.Count)
                _controller.RemovePartyMember(_partySnapshot[slotIdx]);
        }

        private VisualElement BuildInventoryRow(int index, InventorySlot slot, bool isTown)
        {
            var row = new Button(() =>
            {
                if (isTown) _controller.TownToSupp(index, slot);
                else _controller.SuppToTown(index, slot);
            });
            row.AddToClassList("embark__inventory-row");

            var icon = new VisualElement();
            icon.AddToClassList("embark__inventory-icon");
            if (slot.item.icon != null)
                icon.style.backgroundImage = new StyleBackground(slot.item.icon);

            var nameLabel = new Label(slot.item.DisplayName);
            nameLabel.AddToClassList("embark__inventory-name");

            var qtyLabel = new Label($"x{slot.quantity}");
            qtyLabel.AddToClassList("embark__inventory-qty");

            row.Add(icon);
            row.Add(nameLabel);
            row.Add(qtyLabel);
            return row;
        }

        private void BuildRegionList()
        {
            if (_regionList == null) return;
            _regionList.Clear();
            _regionCards.Clear();

            foreach (var region in _controller.GetRegions())
            {
                var card = new Button(() => _controller.SelectRegion(region));
                card.AddToClassList("embark__region-card");

                var nameLabel = new Label(region.DisplayName);
                nameLabel.AddToClassList("embark__region-card__name");

                var metaLabel = new Label(region.Meta);
                metaLabel.AddToClassList("embark__region-card__meta");

                card.Add(nameLabel);
                card.Add(metaLabel);
                _regionList.Add(card);
                _regionCards[region.Id] = card;
            }
        }

        private void RefreshSelectedRegion(ExpeditionRegion region)
        {
            foreach (var kv in _regionCards)
                kv.Value.EnableInClassList("embark__region-card--selected", region != null && kv.Key == region.Id);

            if (region != null)
                SetDestinationInfo(region.DisplayName, region.Meta);
            else
                SetDestinationInfo("목적지 미선택", "");

            ClearEmbarkWarning();
        }

        private void ShowEmbarkWarning()
        {
            if (_step2Hint == null) return;
            _step2Hint.text = "탐사 지역을 먼저 선택하세요.";
            _step2Hint.AddToClassList("embark__footer-hint--warning");
        }

        private void ClearEmbarkWarning()
        {
            if (_step2Hint == null) return;
            _step2Hint.RemoveFromClassList("embark__footer-hint--warning");
            _step2Hint.text = _defaultStep2Hint;
        }

        private async UniTaskVoid LoadAvatarAsync(string address, VisualElement target)
        {
            var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
            if (sprite != null)
                target.style.backgroundImage = new StyleBackground(sprite);
        }
    }
}
