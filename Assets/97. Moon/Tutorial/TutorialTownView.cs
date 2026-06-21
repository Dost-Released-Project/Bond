using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem; 
using VContainer;

namespace Bond.Tutorial
{
    public class TutorialTownView : MonoBehaviour
    {
        private TutorialSystemController _controller;

        [SerializeField] private UIDocument townUiDocument;
        [Header("카메라 (월드 좌표 변환용)")]
        [SerializeField] private Camera mainCamera;

        private VisualElement _root;
        private VisualElement _barrier;
        private VisualElement _focusBox;
        private Label _guideLabel;
        private Button _skipButton;

        private VisualElement _maskTop;
        private VisualElement _maskBottom;
        private VisualElement _maskLeft;
        private VisualElement _maskRight;

        // 상태 제어 데이터
        private string _targetKey;
        private Rect _calculatedBounds;
        private bool _isTrackingWorld;
        private bool _isStepPendingAdvance;
        private TutorialStepSO _currentStepData;

        [Inject]
        public void Construct(TutorialSystemController controller)
        {
            _controller = controller;
        }

        private void Start()
        {
            if (townUiDocument == null) return;
            if (mainCamera == null) mainCamera = Camera.main;

            _root = townUiDocument.rootVisualElement;
            _barrier = _root.Q<VisualElement>("TutorialBarrier");
            _focusBox = _barrier?.Q<VisualElement>("FocusBox");
            _guideLabel = _root.Q<Label>("GuideTextLabel");
            _skipButton = _root.Q<Button>("SkipButton");

            _maskTop = _barrier?.Q<VisualElement>("Mask_Top");
            _maskBottom = _barrier?.Q<VisualElement>("Mask_Bottom");
            _maskLeft = _barrier?.Q<VisualElement>("Mask_Left");
            _maskRight = _barrier?.Q<VisualElement>("Mask_Right");

            if (_barrier != null) _barrier.pickingMode = PickingMode.Ignore;
            if (_focusBox != null) _focusBox.pickingMode = PickingMode.Ignore;
            if (_guideLabel != null) _guideLabel.pickingMode = PickingMode.Ignore;

            if (_maskTop != null) _maskTop.pickingMode = PickingMode.Position;
            if (_maskBottom != null) _maskBottom.pickingMode = PickingMode.Position;
            if (_maskLeft != null) _maskLeft.pickingMode = PickingMode.Position;
            if (_maskRight != null) _maskRight.pickingMode = PickingMode.Position;
            if (_skipButton != null) _skipButton.pickingMode = PickingMode.Position;

            if (_skipButton != null) _skipButton.clicked += OnSkipButtonTriggered;

            // 💥 [핵심 개혁] 잘못된 클릭 유형이 하부 인게임 UI를 자극하기 전에 '이벤트 터널링 단계'에서 가로챕니다.
            _root.panel.visualTree.RegisterCallback<PointerDownEvent>(OnCapturedPointerDown, TrickleDown.TrickleDown);
            _root.panel.visualTree.RegisterCallback<PointerUpEvent>(OnCapturedPointerUp, TrickleDown.TrickleDown);

            _controller.OnStepChanged += OnStepUpdated;
            _controller.OnTutorialFinished += OnTutorialCleared;

            _controller.LoadProgress();
        }

        private void Update()
        {
            if (_barrier == null || _barrier.style.display == DisplayStyle.None || string.IsNullOrEmpty(_targetKey)) return;

            if (_isTrackingWorld)
            {
                GameObject targetObj = GameObject.Find(_targetKey);
                if (targetObj != null)
                {
                    _calculatedBounds = CalculateWorldBoundsToScreen(targetObj);
                    ApplyLayout(_calculatedBounds);
                }
            }
            else
            {
                VisualElement targetUi = FindTargetVisualElement(_targetKey);
                if (targetUi != null)
                {
                    Rect rect = targetUi.worldBound;
                    if (!float.IsNaN(rect.width) && rect.width > 0 && rect.x > -1000 && rect.y > -1000)
                    {
                        _calculatedBounds = rect;
                        ApplyLayout(rect);
                    }
                }
            }
        }

        // 💥 [인게임 클릭 원천 차단기 1] 누르는 순간 필터링
        private void OnCapturedPointerDown(PointerDownEvent evt)
        {
            if (_barrier == null || _barrier.style.display == DisplayStyle.None) return;
            if (_skipButton != null && _skipButton.worldBound.Contains(evt.position)) return;

            // 허용되지 않은 마우스 입력이 들어왔을 때 이벤트 전파 자체를 말소시킵니다.
            if (!IsValidClickRequirement(evt.button))
            {
                evt.StopPropagation();
            }
        }

        // 💥 [인게임 클릭 원천 차단기 2] 뗄 때도 동일하게 차단하여 완벽 방어
        private void OnCapturedPointerUp(PointerUpEvent evt)
        {
            if (_barrier == null || _barrier.style.display == DisplayStyle.None) return;
            if (_skipButton != null && _skipButton.worldBound.Contains(evt.position)) return;

            if (!IsValidClickRequirement(evt.button))
            {
                evt.StopPropagation();
            }
        }

        // 💥 클릭 조건 일치 여부 정밀 검증기
        private bool IsValidClickRequirement(int mouseButton)
        {
            if (mouseButton != 0 && mouseButton != 1) return false;

            TutorialClickType inputClickType = mouseButton == 0 ? TutorialClickType.LeftClick : TutorialClickType.RightClick;
            TutorialClickType requiredClickType = _currentStepData != null ? _currentStepData.ClickType : TutorialClickType.AnyClick;

            if (requiredClickType != TutorialClickType.AnyClick && inputClickType != requiredClickType)
            {
                return false; // 조건 불일치
            }
            return true; // 정상 유효 클릭
        }

        // 전역 물리 마우스 해제 시점에 단약을 안전하게 전진시키는 감시 루프
        private void HandleUniversalInputTracking()
        {
            if (_isStepPendingAdvance) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;

            bool leftReleased = mouse.leftButton.wasReleasedThisFrame;
            bool rightReleased = mouse.rightButton.wasReleasedThisFrame;

            if (leftReleased || rightReleased)
            {
                int currentButton = leftReleased ? 0 : 1;
                
                // 이번 프레임 물리 입력이 데이터 요구 조건과 다르면 단약 전진 프로세스 패스
                if (!IsValidClickRequirement(currentButton)) return;

                Vector2 mousePos = mouse.position.ReadValue();
                Vector2 localMousePos = new Vector2(mousePos.x, Screen.height - mousePos.y);

                if (_skipButton != null && _skipButton.worldBound.Contains(localMousePos)) return;

                if (_isTrackingWorld)
                {
                    if (_calculatedBounds.Contains(localMousePos))
                    {
                        TriggerAdvanceDeferred();
                    }
                }
                else
                {
                    VisualElement expectedTarget = FindTargetVisualElement(_targetKey);
                    if (expectedTarget != null)
                    {
                        if (_calculatedBounds.Contains(localMousePos))
                        {
                            TriggerAdvanceDeferred();
                            return;
                        }

                        VisualElement pickedElement = _root.panel.Pick(localMousePos);
                        if (pickedElement != null && (pickedElement == expectedTarget || expectedTarget.Contains(pickedElement)))
                        {
                            TriggerAdvanceDeferred();
                        }
                    }
                }
            }
        }

        private void LateUpdate()
        {
            // Update 단계에서 좌표 계산 및 차단이 끝난 후 안전하게 입력 상태를 감시합니다.
            if (_barrier != null && _barrier.style.display == DisplayStyle.Flex && !string.IsNullOrEmpty(_targetKey))
            {
                HandleUniversalInputTracking();
            }
        }

        private void TriggerAdvanceDeferred()
        {
            _isStepPendingAdvance = true;
            _barrier.schedule.Execute(() =>
            {
                _controller.Advance();
            }).ExecuteLater(20);
        }

        private void OnStepUpdated(TutorialStepSO stepData)
        {
            _currentStepData = stepData; 

            if (stepData.Sequence != TutorialSequence.Sequence_A_Town)
            {
                if (_barrier != null) _barrier.style.display = DisplayStyle.None;
                return;
            }

            if (_barrier == null) return;

            _barrier.style.display = DisplayStyle.Flex;
            _targetKey = stepData.TargetUiKey;
            _calculatedBounds = new Rect(0, 0, 0, 0);
            _isStepPendingAdvance = false;

            _isTrackingWorld = !string.IsNullOrEmpty(_targetKey) && !_targetKey.Contains(".") && GameObject.Find(_targetKey) != null;

            if (_guideLabel != null) _guideLabel.text = stepData.Description;
        }

        private VisualElement FindTargetVisualElement(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            if (key.Contains("."))
            {
                string[] split = key.Split('.');
                string docGoName = split[0].Trim();
                string elName = split[1].Trim();

                GameObject docGo = GameObject.Find(docGoName);
                if (docGo != null && docGo.TryGetComponent<UIDocument>(out var specDoc))
                {
                    if (specDoc.rootVisualElement != null)
                        return specDoc.rootVisualElement.Q<VisualElement>(elName);
                }
            }

            var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in uiDocuments)
            {
                if (doc != null && doc != townUiDocument && doc.rootVisualElement != null)
                {
                    var el = doc.rootVisualElement.Q<VisualElement>(key);
                    if (el != null) return el;
                }
            }
            return null;
        }

        private void ApplyLayout(Rect bounds)
        {
            float sw = Screen.width;
            float sh = Screen.height;

            if (_focusBox != null)
            {
                _focusBox.style.position = Position.Absolute;
                _focusBox.style.left = bounds.x;
                _focusBox.style.top = bounds.y;
                _focusBox.style.width = bounds.width;
                _focusBox.style.height = bounds.height;
            }

            if (_maskTop != null) { _maskTop.style.left = 0; _maskTop.style.top = 0; _maskTop.style.width = sw; _maskTop.style.height = bounds.y; }
            if (_maskBottom != null) { _maskBottom.style.left = 0; _maskBottom.style.top = bounds.y + bounds.height; _maskBottom.style.width = sw; _maskBottom.style.height = sh - (bounds.y + bounds.height); }
            if (_maskLeft != null) { _maskLeft.style.left = 0; _maskLeft.style.top = bounds.y; _maskLeft.style.width = bounds.x; _maskLeft.style.height = bounds.height; }
            if (_maskRight != null) { _maskRight.style.left = bounds.x + bounds.width; _maskRight.style.top = bounds.y; _maskRight.style.width = sw - (bounds.x + bounds.width); _maskRight.style.height = bounds.height; }

            if (_guideLabel != null)
            {
                _guideLabel.style.position = Position.Absolute;
                _guideLabel.style.width = 450;
                float centerLeft = bounds.x + (bounds.width / 2f) - 225f;
                _guideLabel.style.left = Mathf.Clamp(centerLeft, 30f, sw - 480f);

                if (bounds.y + bounds.height + 140f > sh) _guideLabel.style.top = Mathf.Max(10f, bounds.y - 110f);
                else _guideLabel.style.top = bounds.y + bounds.height + 20f;
            }
        }

        private Rect CalculateWorldBoundsToScreen(GameObject targetGo)
        {
            Bounds objectBounds = new Bounds(targetGo.transform.position, Vector3.one * 1.5f);
            if (targetGo.TryGetComponent<Collider>(out var collider)) objectBounds = collider.bounds;
            else if (targetGo.TryGetComponent<SpriteRenderer>(out var renderer)) objectBounds = renderer.bounds;

            Vector3 minScreen = mainCamera.WorldToScreenPoint(objectBounds.min);
            Vector3 maxScreen = mainCamera.WorldToScreenPoint(objectBounds.max);

            float x = Mathf.Min(minScreen.x, maxScreen.x);
            float y = Screen.height - Mathf.Max(minScreen.y, maxScreen.y);
            return new Rect(x, y, Mathf.Abs(maxScreen.x - minScreen.x), Mathf.Abs(maxScreen.y - minScreen.y));
        }

        private void OnSkipButtonTriggered()
        {
            var buildingDB = DBSORegistry.GetDb<BuildingDataBaseSO>();
            BuildingData supplySO = buildingDB?.FindSO<BuildingData>(d => d.buildingType == BuildingType.Supply);
            BuildingData storageSO = buildingDB?.FindSO<BuildingData>(d => d.buildingType == BuildingType.Storage);
            _controller.Skip(supplySO, storageSO);
        }

        private void OnTutorialCleared()
        {
            if (_barrier != null) _barrier.style.display = DisplayStyle.None;
            UnregisterGlobalCallbacks();
        }

        private void UnregisterGlobalCallbacks()
        {
            if (_root?.panel?.visualTree != null)
            {
                _root.panel.visualTree.UnregisterCallback<PointerDownEvent>(OnCapturedPointerDown, TrickleDown.TrickleDown);
                _root.panel.visualTree.UnregisterCallback<PointerUpEvent>(OnCapturedPointerUp, TrickleDown.TrickleDown);
            }
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnStepChanged -= OnStepUpdated;
                _controller.OnTutorialFinished -= OnTutorialCleared;
            }
            UnregisterGlobalCallbacks();
        }
    }
}