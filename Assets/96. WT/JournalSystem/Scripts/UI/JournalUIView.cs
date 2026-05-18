using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [World Visualizer] 일지 UI의 시각적 연출 제어 (UI Toolkit 구현체)
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class JournalUIView : MonoBehaviour, IJournalVisualizer
    {
        [Header("UI Toolkit Settings")]
        [Tooltip("선택지 버튼 한 개를 구성하는 UXML 템플릿")]
        [SerializeField] private VisualTreeAsset _optionButtonTemplate;
        [SerializeField] private float _typingSpeed = 0.05f;

        // UI Elements
        private VisualElement _rootPanel;
        private Label _contentLabel;
        private VisualElement _entryIconImage;
        private VisualElement _optionButtonContainer;
        private Button _nextButton;
        private Button _prevButton;

        // IJournalVisualizer 인터페이스 구현
        public Action OnNextClicked { get; set; }
        public Action OnPrevClicked { get; set; }
        public Action<JournalOption> OnOptionSelected { get; set; }

        private Coroutine _typingCoroutine;

        private void OnEnable()
        {
            var uiDocument = GetComponent<UIDocument>();
            var root = uiDocument.rootVisualElement;

            if (root == null) return;

            // UXML에 정의된 Name(#)으로 요소 쿼리
            _rootPanel = root.Q<VisualElement>("RootPanel");
            _contentLabel = root.Q<Label>("ContentLabel");
            _entryIconImage = root.Q<VisualElement>("IconImage");
            _optionButtonContainer = root.Q<VisualElement>("OptionContainer");
            _nextButton = root.Q<Button>("NextButton");
            _prevButton = root.Q<Button>("PrevButton");

            if (_nextButton != null)
            {
                _nextButton.clicked += () => OnNextClicked?.Invoke();
            }
            
            if (_prevButton != null)
            {
                _prevButton.clicked += () => OnPrevClicked?.Invoke();
            }

            // 시작 시 UI 숨김
            SetVisible(false);
        }

        public void SetPrevButtonEnabled(bool isEnabled)
        {
            if (_prevButton != null)
            {
                // UI Toolkit에서 상호작용 가능 여부를 제어하는 코드
                // isEnabled가 false면 버튼이 비활성화(회색)됩니다.
                _prevButton.SetEnabled(isEnabled); 
            }
        }

        public void SetNextButtonText(string text)
        {
            if (_nextButton != null)
            {
                _nextButton.text = text;
            }
        }

        public void SetVisible(bool isVisible)
        {
            if (_rootPanel != null)
            {
                _rootPanel.style.display = isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            }
        }

        public void ClearUI()
        {
            if (_contentLabel != null) _contentLabel.text = string.Empty;
            if (_entryIconImage != null) _entryIconImage.style.display = DisplayStyle.None;
            
            if (_optionButtonContainer != null)
            {
                _optionButtonContainer.Clear();
            }
            
            if (_nextButton != null) _nextButton.style.display = DisplayStyle.None;
        }

        public void ShowText(string text, bool isTyping = true)
        {
            if (_typingCoroutine != null) StopCoroutine(_typingCoroutine);
            
            if (isTyping && gameObject.activeInHierarchy)
            {
                _typingCoroutine = StartCoroutine(Co_TypeText(text));
            }
            else
            {
                if (_contentLabel != null) _contentLabel.text = text;
            }
        }

        private IEnumerator Co_TypeText(string text)
        {
            if (_contentLabel == null) yield break;

            _contentLabel.text = string.Empty;
            foreach (char c in text)
            {
                _contentLabel.text += c;
                yield return new WaitForSeconds(_typingSpeed);
            }
            _typingCoroutine = null;
            if (_nextButton != null) _nextButton.style.display = DisplayStyle.Flex;
        }

        public void SetIcon(Sprite icon)
        {
            if (_entryIconImage == null) return;

            if (icon != null)
            {
                _entryIconImage.style.backgroundImage = new StyleBackground(icon);
                _entryIconImage.style.display = DisplayStyle.Flex;
            }
            else
            {
                _entryIconImage.style.display = DisplayStyle.None;
            }
        }

        public void SetOptions(IReadOnlyList<JournalOption> options)
        {
            if (_optionButtonContainer == null) return;

            _optionButtonContainer.Clear();

            if (options == null || options.Count == 0) return;

            foreach (var option in options)
            {
                if (_optionButtonTemplate == null)
                {
                    Debug.LogWarning("[JournalUIView] Option Button Template 이 비어있습니다.");
                    continue;
                }

                // 템플릿 인스턴스화
                TemplateContainer buttonInstance = _optionButtonTemplate.Instantiate();
                var btn = buttonInstance.Q<Button>(); // 템플릿 내부의 루트가 Button이거나, 자식 중 Button을 찾음
                
                if (btn != null)
                {
                    btn.text = option.text;
                    btn.clicked += () => OnOptionSelected?.Invoke(option);
                }

                _optionButtonContainer.Add(buttonInstance);
            }
            
            if (_nextButton != null) _nextButton.style.display = DisplayStyle.None;
        }
    }
}
