using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.UI.PartySelection
{
    public class RosterPanelPresenter
    {
        public event Action<BaseCharacter> OnCharacterSelected;

        private readonly ScrollView    _grid;
        private readonly Button        _filterAll;
        private readonly Button        _filterWarrior;
        private readonly Button        _filterRogue;
        private readonly Button        _filterCleric;

        private readonly List<CardViewModel> _cards = new();
        private Button _activeFilter;
        private string _currentFilter = ""; // "" = 전체

        private class CardViewModel
        {
            public BaseCharacter Character;
            public VisualElement Root;
            public string        ClassKey; // "Warrior" / "Assassin" / "Cleric"
        }

        public RosterPanelPresenter(VisualElement root)
        {
            _grid          = root.Q<ScrollView>("rosterGrid");
            _filterAll     = root.Q<Button>("filterAll");
            _filterWarrior = root.Q<Button>("filterWarrior");
            _filterRogue   = root.Q<Button>("filterRogue");
            _filterCleric  = root.Q<Button>("filterCleric");

            // 카드를 가로부터 채우도록 contentContainer에 직접 적용
            _grid.contentContainer.style.flexDirection = FlexDirection.Row;
            _grid.contentContainer.style.flexWrap      = Wrap.Wrap;

            _activeFilter = _filterAll;

            _filterAll.clicked     += () => SetFilter("",        _filterAll);
            _filterWarrior.clicked += () => SetFilter("Warrior", _filterWarrior);
            _filterRogue.clicked   += () => SetFilter("Assassin",_filterRogue);
            _filterCleric.clicked  += () => SetFilter("Cleric",  _filterCleric);
        }

        public void Bind(IReadOnlyList<BaseCharacter> roster)
        {
            _grid.Clear();
            _cards.Clear();

            foreach (var character in roster)
            {
                var vm = BuildCard(character);
                _cards.Add(vm);
                _grid.Add(vm.Root);
            }
        }

        // 외부(Controller)에서 편성됨/해제 상태 동기화
        // 카드는 항상 활성화 상태 유지 — 편성된 카드 클릭 시 Controller가 해제 처리
        public void SetSelected(BaseCharacter character, bool selected)
        {
            var vm = _cards.Find(c => c.Character == character);
            if (vm == null) return;

            if (selected)
                vm.Root.AddToClassList("roster-card--selected");
            else
                vm.Root.RemoveFromClassList("roster-card--selected");
        }

        private void SetFilter(string classKey, Button btn)
        {
            _activeFilter?.RemoveFromClassList("filter-btn--active");
            btn.AddToClassList("filter-btn--active");
            _activeFilter  = btn;
            _currentFilter = classKey;
            ApplyFilter();
        }

        private void ApplyFilter()
        {
            foreach (var vm in _cards)
            {
                bool visible = string.IsNullOrEmpty(_currentFilter) ||
                               vm.ClassKey == _currentFilter;
                vm.Root.style.display = visible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        private CardViewModel BuildCard(BaseCharacter character)
        {
            var stat        = character.StatComponent;
            string classKey = stat != null ? stat.ClassType.ToString() : "";
            Debug.Log($"is stat null? {stat == null}");
            Debug.Log(classKey);

            bool isDanger = stat != null &&
                            ((float)stat.current_Hp / Mathf.Max(1, stat.max_Hp) <= 0.3f ||
                             stat.insanity >= 80);

            var root = new Button(() => OnCharacterSelected?.Invoke(character));
            root.AddToClassList("roster-card");
            if (isDanger) root.AddToClassList("roster-card--danger");

            // 위험 점 (absolute — 가장 먼저 추가해야 다른 요소 위에 표시됨)
            if (isDanger)
            {
                var dot = new VisualElement();
                dot.AddToClassList("danger-dot");
                root.Add(dot);
            }

            // ── 상단 행: 아바타 + 이름/직업 ──
            var charTop = new VisualElement();
            charTop.AddToClassList("roster-card__top");

            var avatar = new VisualElement();
            avatar.AddToClassList("roster-card__avatar");

            var charMeta = new VisualElement();
            charMeta.AddToClassList("roster-card__meta");

            var nameLabel = new Label(character.UnitName);
            nameLabel.AddToClassList("char-name");

            string classDisplay = classKey switch
            {
                "Warrior"  => "전사",
                "Assassin" => "도적",
                "Cleric"   => "성직자",
                _          => classKey
            };
            var classLabel = new Label($"{classDisplay} · Lv.{character.level}");
            classLabel.AddToClassList("char-class");

            charMeta.Add(nameLabel);
            charMeta.Add(classLabel);
            charTop.Add(avatar);
            charTop.Add(charMeta);

            // ── 게이지 행 ──
            var barsContainer = new VisualElement();
            barsContainer.AddToClassList("roster-card__bars");

            // HP 행
            var hpRow   = new VisualElement();
            hpRow.AddToClassList("bar-row");
            var hpLabel = new Label("HP");
            hpLabel.AddToClassList("bar-label");
            var hpTrack = new VisualElement();
            hpTrack.AddToClassList("bar-track");
            var hpFill  = new VisualElement();
            hpFill.AddToClassList("bar-fill");

            if (stat != null && stat.max_Hp > 0)
            {
                float hpRatio = (float)stat.current_Hp / stat.max_Hp;
                hpFill.style.width = Length.Percent(hpRatio * 100f);
                hpFill.AddToClassList(hpRatio > 0.3f ? "bar-fill--hp-safe" : "bar-fill--hp-low");
            }
            hpTrack.Add(hpFill);
            hpRow.Add(hpLabel);
            hpRow.Add(hpTrack);

            // 스트레스 행
            var stressRow   = new VisualElement();
            stressRow.AddToClassList("bar-row");
            var stressLabel = new Label("ST");
            stressLabel.AddToClassList("bar-label");
            var stressTrack = new VisualElement();
            stressTrack.AddToClassList("bar-track");
            var stressFill  = new VisualElement();
            stressFill.AddToClassList("bar-fill");

            if (stat != null)
            {
                float stressRatio = stat.insanity / 100f;
                stressFill.style.width = Length.Percent(stressRatio * 100f);
                string stressClass = stat.insanity >= 80 ? "bar-fill--stress-crit"
                                   : stat.insanity >= 50 ? "bar-fill--stress-warn"
                                                         : "bar-fill--stress-safe";
                stressFill.AddToClassList(stressClass);
            }
            stressTrack.Add(stressFill);
            stressRow.Add(stressLabel);
            stressRow.Add(stressTrack);

            barsContainer.Add(hpRow);
            barsContainer.Add(stressRow);

            // ── 성향 태그 ──
            var traitTags = new VisualElement();
            traitTags.AddToClassList("trait-tags");
            foreach (var trait in character.traits)
            {
                if (trait == null || string.IsNullOrEmpty(trait.Name)) continue;
                var tag = new Label(trait.Name);
                tag.AddToClassList("trait-tag");
                traitTags.Add(tag);
            }

            root.Add(charTop);
            root.Add(barsContainer);
            root.Add(traitTags);

            return new CardViewModel { Character = character, Root = root, ClassKey = classKey };
        }
    }
}