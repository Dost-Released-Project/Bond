using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 이벤트 선택지 하나를 표현하는 UI 컴포넌트.
/// EventSceneController.BuildChoiceButtons() 에서 동적으로 Instantiate 된다.
///
/// Inspector 연결 필요:
///   _button    — 클릭을 받는 UI Button
///   _labelText — 선택지 텍스트를 표시할 TextMeshProUGUI
/// </summary>
public class EventChoiceButton : MonoBehaviour
{
    [SerializeField] private Button _button;
    [SerializeField] private TextMeshProUGUI _labelText;

    private Action _onClickCallback;

    /// <summary>
    /// 선택지 데이터와 클릭 콜백을 받아 이 버튼을 초기화한다.
    /// </summary>
    /// <param name="choice">표시할 EventChoice 데이터.</param>
    /// <param name="onClickCallback">버튼 클릭 시 호출할 콜백.</param>
    public void Setup(EventChoice choice, Action onClickCallback)
    {
        if (_button == null)
        {
            Debug.LogError("[EventChoiceButton] _button 이 연결되지 않았습니다.", this);
            return;
        }

        _onClickCallback = onClickCallback;

        if (_labelText == null)
            Debug.LogWarning("[EventChoiceButton] _labelText 가 연결되지 않았습니다.", this);
        else
            _labelText.text = choice.Label;

        // 중복 등록 방지 후 클릭 이벤트 연결
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnButtonClicked);
    }

    /// <summary>
    /// 버튼의 인터랙션 가능 여부를 설정한다.
    /// 선택지 클릭 후 중복 입력을 방지하기 위해 EventSceneController 에서 호출한다.
    /// </summary>
    /// <param name="interactable">true 면 활성화, false 면 비활성화.</param>
    public void SetInteractable(bool interactable)
    {
        if (_button == null)
            return;

        _button.interactable = interactable;
    }

    private void OnButtonClicked()
    {
        _onClickCallback?.Invoke();
    }

    private void OnDestroy()
    {
        if (_button != null)
            _button.onClick.RemoveListener(OnButtonClicked);
    }
}
