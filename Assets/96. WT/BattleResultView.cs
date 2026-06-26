using System;
using System.Collections.Generic;
using BattleSystem.Interface;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.WT.Journal
{
    [RequireComponent(typeof(UIDocument))]
    public class BattleResultView : MonoBehaviour
    {
        private VisualElement _root;
        private Label _titleText;
        private VisualElement _partyContainer;
        private Label _valueFrontier;
        private Label _valueWood;
        private Label _valueOre;
        private Button _btnReturn;

        private bool _isInitialized = false;

        private void Start()
        {
            EnsureInitialized();
            Hide();
        }

        private bool EnsureInitialized()
        {
            if (_isInitialized) return true;

            var uiDocument = GetComponent<UIDocument>();
            if (uiDocument == null) return false;

            var rootVisual = uiDocument.rootVisualElement;
            if (rootVisual == null) return false;

            _root = rootVisual.Q<VisualElement>("Root");
            _titleText = rootVisual.Q<Label>("TitleText");
            _partyContainer = rootVisual.Q<VisualElement>("PartyContainer");
            _valueFrontier = rootVisual.Q<Label>("ValueFrontier");
            _valueWood = rootVisual.Q<Label>("ValueWood");
            _valueOre = rootVisual.Q<Label>("ValueOre");
            _btnReturn = rootVisual.Q<Button>("BtnReturn");

            _isInitialized = _root != null;
            return _isInitialized;
        }

        public void Hide()
        {
            EnsureInitialized();
            if (_root != null)
            {
                _root.style.display = DisplayStyle.None;
            }
        }

        public void ShowResult(
            BattleEndStatus status, 
            IReadOnlyList<BaseCharacter> party, 
            int frontier, 
            int wood, 
            int ore, 
            Dictionary<string, Sprite> characterPortraits,
            Action onReturnClicked)
        {
            if (!EnsureInitialized())
            {
                Debug.LogError("[BattleResultView] UIDocument rootVisualElement 초기화 실패로 결과창을 표시할 수 없습니다.");
                return;
            }

            // 1. 타이틀 설정
            _titleText.ClearClassList();
            _titleText.AddToClassList("result-title");

            switch (status)
            {
                case BattleEndStatus.Victory:
                    _titleText.text = "전투 승리";
                    _titleText.AddToClassList("victory");
                    break;
                case BattleEndStatus.Defeat:
                    _titleText.text = "전투 패배";
                    _titleText.AddToClassList("defeat");
                    break;
                case BattleEndStatus.Retreat:
                    _titleText.text = "퇴각 완료";
                    _titleText.AddToClassList("retreat");
                    break;
            }

            // 2. 파티 컨테이너 클리어 및 생성
            _partyContainer.Clear();
            if (party != null)
            {
                foreach (var character in party)
                {
                    if (character == null) continue;

                    // 카드 생성
                    var card = new VisualElement();
                    card.AddToClassList("character-card");

                    // 초상화
                    var portrait = new VisualElement();
                    portrait.AddToClassList("character-portrait");
                    if (characterPortraits != null && characterPortraits.TryGetValue(character.Id, out Sprite sp) && sp != null)
                    {
                        portrait.style.backgroundImage = new StyleBackground(sp);
                    }
                    card.Add(portrait);

                    // 이름
                    var nameLabel = new Label(character.Name);
                    nameLabel.AddToClassList("character-name");
                    card.Add(nameLabel);

                    // HP Bar
                    var hpContainer = new VisualElement();
                    hpContainer.AddToClassList("bar-container");
                    var hpBar = new VisualElement();
                    hpBar.AddToClassList("hp-bar");
                    float hpPercent = character.Stat.max_Hp > 0 
                        ? (float)character.Stat.current_Hp / character.Stat.max_Hp * 100f 
                        : 0f;
                    hpBar.style.width = Length.Percent(hpPercent);
                    var hpLabel = new Label($"HP {character.Stat.current_Hp}/{character.Stat.max_Hp}");
                    hpLabel.AddToClassList("bar-label");
                    hpContainer.Add(hpBar);
                    hpContainer.Add(hpLabel);
                    card.Add(hpContainer);

                    // Stress (Insanity) Bar
                    var stressContainer = new VisualElement();
                    stressContainer.AddToClassList("bar-container");
                    var stressBar = new VisualElement();
                    stressBar.AddToClassList("stress-bar");
                    float stressPercent = Mathf.Clamp(character.Insanity, 0, 100);
                    stressBar.style.width = Length.Percent(stressPercent);
                    var stressLabel = new Label($"STRESS {character.Insanity}/100");
                    stressLabel.AddToClassList("bar-label");
                    stressContainer.Add(stressBar);
                    stressContainer.Add(stressLabel);
                    card.Add(stressContainer);

                    _partyContainer.Add(card);
                }
            }

            // 3. 보상 설정
            _valueFrontier.text = $"+{frontier}";
            _valueWood.text = $"+{wood}";
            _valueOre.text = $"+{ore}";

            // 4. 리턴 버튼 설정
            if (_btnReturn != null)
            {
                switch (status)
                {
                    case BattleEndStatus.Victory:
                        _btnReturn.text = "맵으로 복귀";
                        break;
                    case BattleEndStatus.Defeat:
                        _btnReturn.text = "마을로 귀환";
                        break;
                    case BattleEndStatus.Retreat:
                        _btnReturn.text = "맵으로 퇴각";
                        break;
                }
                _btnReturn.clickable = new Clickable(() => onReturnClicked?.Invoke());
            }

            _root.style.display = DisplayStyle.Flex;
        }
    }
}
