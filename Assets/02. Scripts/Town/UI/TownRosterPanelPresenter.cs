using System;
using System.Collections.Generic;
using System.Linq;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.UIElements;

namespace Bond.UI.Town
{
    public class TownRosterPanelPresenter
    {
        private readonly VisualElement _panel;
        private readonly Label _countLabel;
        private readonly VisualElement _list;
        private readonly Roster _roster;
        private readonly CharacterSelector _selector;

        private readonly List<CardViewModel> _cards = new();

        public bool IsVisible { get; private set; }

        // Embark 연동용 콜백 — 기본값 null (일반 타운 모드에서는 미사용)
        public Action<BaseCharacter> OnCardClicked;
        public Action<BaseCharacter> OnCardRightClicked;

        private class CardViewModel
        {
            public BaseCharacter Character;
            public VisualElement Root;
        }

        public TownRosterPanelPresenter(VisualElement root, Roster roster, CharacterSelector selector)
        {
            _roster = roster;
            _selector = selector;

            _panel      = root.Q("roster-panel");
            _countLabel = root.Q<Label>("roster-panel__count");
            _list       = root.Q("roster-panel__list");

            var scroll = root.Q<ScrollView>("roster-panel__scroll");
            scroll.contentViewport.style.backgroundColor = StyleKeyword.None;

            root.Q<Button>("roster-panel__close-btn").clicked += Hide;

            _selector.OnSelectionChanged += _ => UpdateSelectedVisual();
        }

        public void Show()
        {
            _panel.RemoveFromClassList("roster-panel--hidden");
            _panel.AddToClassList("roster-panel--visible");
            IsVisible = true;
            RefreshList();
        }

        public void Hide()
        {
            _panel.RemoveFromClassList("roster-panel--visible");
            _panel.AddToClassList("roster-panel--hidden");
            IsVisible = false;
        }

        private void RefreshList()
        {
            _list.Clear();
            _cards.Clear();

            foreach (var character in _roster.Characters)
            {
                var vm = BuildCard(character);
                _cards.Add(vm);
                _list.Add(vm.Root);
            }

            _countLabel.text = $"{_roster.Characters.Count}명";
            UpdateSelectedVisual();
        }

        public void UpdatePartyHighlight(IReadOnlyList<BaseCharacter> party)
        {
            foreach (var vm in _cards)
            {
                bool inParty = party != null && party.Contains(vm.Character);
                vm.Root.EnableInClassList("roster-card--selected", inParty);
            }
        }

        private CardViewModel BuildCard(BaseCharacter character)
        {
            var stat     = character.Stat;
            bool isDanger = stat != null &&
                            ((float)stat.current_Hp / Mathf.Max(1, stat.max_Hp) <= 0.3f ||
                             character.Insanity >= 80);

            var card = new Button(() =>
            {
                _selector.ToggleSelection(character);
                OnCardClicked?.Invoke(character);
            });
            card.RegisterCallback<PointerDownEvent>(evt =>
            {
                if (evt.button != 1) return;
                OnCardRightClicked?.Invoke(character);
                evt.StopPropagation();
            });
            card.AddToClassList("roster-card");
            if (isDanger) card.AddToClassList("roster-card--danger");

            if (isDanger)
            {
                var dot = new VisualElement();
                dot.AddToClassList("roster-card__danger-dot");
                card.Add(dot);
            }

            // 상단: 아바타 + 이름/직업
            var top = new VisualElement();
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems    = Align.FlexStart;
            top.style.marginBottom  = 8;

            var avatar = new VisualElement();
            avatar.AddToClassList("roster-card__avatar");
            LoadAvatarAsync(character.ImageAddress, avatar).Forget();

            var meta = new VisualElement();
            meta.style.flexGrow      = 1;
            meta.style.flexDirection = FlexDirection.Column;

            var nameLabel = new Label(character.Name);
            nameLabel.AddToClassList("roster-card__name");

            var classLabel = new Label($"{character.Profession?.Name ?? ""} · Lv.{character.Level}");
            classLabel.AddToClassList("roster-card__class");

            meta.Add(nameLabel);
            meta.Add(classLabel);
            top.Add(avatar);
            top.Add(meta);

            // 게이지
            var bars = new VisualElement();
            bars.AddToClassList("roster-card__bars");
            bars.Add(BuildBarRow("HP", BuildHpFill(stat)));
            bars.Add(BuildBarRow("ST", BuildStressFill(character)));

            card.Add(top);
            card.Add(bars);

            return new CardViewModel { Character = character, Root = card };
        }

        private VisualElement BuildBarRow(string labelText, VisualElement fill)
        {
            var row = new VisualElement();
            row.AddToClassList("roster-card__bar-row");

            var lbl = new Label(labelText);
            lbl.AddToClassList("roster-card__bar-label");

            var track = new VisualElement();
            track.AddToClassList("roster-card__bar-track");
            track.Add(fill);

            row.Add(lbl);
            row.Add(track);
            return row;
        }

        private VisualElement BuildHpFill(Stat stat)
        {
            var fill = new VisualElement();
            fill.AddToClassList("roster-card__bar-fill");

            if (stat != null && stat.max_Hp > 0)
            {
                float ratio = (float)stat.current_Hp / stat.max_Hp;
                fill.style.width = Length.Percent(ratio * 100f);
                fill.AddToClassList(ratio > 0.3f ? "roster-card__bar-fill--hp-safe"
                                                 : "roster-card__bar-fill--hp-low");
            }
            return fill;
        }

        private VisualElement BuildStressFill(BaseCharacter character)
        {
            var fill = new VisualElement();
            fill.AddToClassList("roster-card__bar-fill");

            float ratio = character.Insanity / 100f;
            fill.style.width = Length.Percent(ratio * 100f);

            string cls = character.Insanity >= 80 ? "roster-card__bar-fill--stress-crit"
                       : character.Insanity >= 50 ? "roster-card__bar-fill--stress-warn"
                                                  : "roster-card__bar-fill--stress-safe";
            fill.AddToClassList(cls);
            return fill;
        }

        private async UniTaskVoid LoadAvatarAsync(string address, VisualElement avatar)
        {
            if (string.IsNullOrEmpty(address)) return;
            var sprite = await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
            if (sprite != null)
                avatar.style.backgroundImage = new StyleBackground(sprite);
        }

        private void UpdateSelectedVisual()
        {
            foreach (var vm in _cards)
            {
                if (_selector.Selected == vm.Character)
                    vm.Root.AddToClassList("roster-card--selected");
                else
                    vm.Root.RemoveFromClassList("roster-card--selected");
            }
        }
    }
}
