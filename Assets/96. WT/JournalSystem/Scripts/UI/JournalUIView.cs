using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [World Visualizer] 일지 UI의 시각적 연출 제어 (UGUI 구현체)
    /// </summary>
    public class JournalUIView : MonoBehaviour, IJournalVisualizer
    {
        [Header("UI Components")]
        [SerializeField] private GameObject _rootPanel; // 전체 UI를 끄고 켤 루트
        [SerializeField] private TextMeshProUGUI _contentText;
        [SerializeField] private Image _entryIconImage;
        [SerializeField] private Transform _optionButtonContainer;
        [SerializeField] private Button _optionButtonPrefab;
        [SerializeField] private Button _nextButton;

        [Header("Settings")]
        [SerializeField] private float _typingSpeed = 0.05f;

        // IJournalVisualizer 인터페이스 구현
        public Action OnNextClicked { get; set; }
        public Action<JournalOption> OnOptionSelected { get; set; }

        private Coroutine _typingCoroutine;

        private void Awake()
        {
            if (_nextButton != null)
                _nextButton.onClick.AddListener(() => OnNextClicked?.Invoke());
        }

        public void SetVisible(bool isVisible)
        {
            if (_rootPanel != null) _rootPanel.SetActive(isVisible);
        }

        public void ClearUI()
        {
            if (_contentText != null) _contentText.text = string.Empty;
            if (_entryIconImage != null) _entryIconImage.gameObject.SetActive(false);
            
            if (_optionButtonContainer != null)
            {
                foreach (Transform child in _optionButtonContainer)
                {
                    Destroy(child.gameObject);
                }
            }
            
            if (_nextButton != null) _nextButton.gameObject.SetActive(false);
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
                if (_contentText != null) _contentText.text = text;
            }
        }

        private IEnumerator Co_TypeText(string text)
        {
            if (_contentText == null) yield break;

            _contentText.text = string.Empty;
            foreach (char c in text)
            {
                _contentText.text += c;
                yield return new WaitForSeconds(_typingSpeed);
            }
            _typingCoroutine = null;
            if (_nextButton != null) _nextButton.gameObject.SetActive(true);
        }

        public void SetIcon(Sprite icon)
        {
            if (_entryIconImage == null) return;

            if (icon != null)
            {
                _entryIconImage.sprite = icon;
                _entryIconImage.gameObject.SetActive(true);
            }
            else
            {
                _entryIconImage.gameObject.SetActive(false);
            }
        }

        public void SetOptions(IReadOnlyList<JournalOption> options)
        {
            if (_optionButtonContainer == null) return;

            foreach (Transform child in _optionButtonContainer)
            {
                Destroy(child.gameObject);
            }

            if (options == null || options.Count == 0) return;

            foreach (var option in options)
            {
                if (_optionButtonPrefab == null) continue;

                var btn = Instantiate(_optionButtonPrefab, _optionButtonContainer);
                var tmp = btn.GetComponentInChildren<TextMeshProUGUI>();
                if (tmp != null) tmp.text = option.text;
                
                btn.onClick.AddListener(() => OnOptionSelected?.Invoke(option));
            }
            
            if (_nextButton != null) _nextButton.gameObject.SetActive(false);
        }
    }
}
