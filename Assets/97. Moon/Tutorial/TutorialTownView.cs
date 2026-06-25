using System.Collections.Generic;
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

            if (_barrier != null) _barrier.pickingMode = PickingMode.Position;
            if (_focusBox != null) _focusBox.pickingMode = PickingMode.Ignore;
            if (_guideLabel != null) _guideLabel.pickingMode = PickingMode.Ignore;

            if (_maskTop != null) _maskTop.pickingMode = PickingMode.Position;
            if (_maskBottom != null) _maskBottom.pickingMode = PickingMode.Position;
            if (_maskLeft != null) _maskLeft.pickingMode = PickingMode.Position;
            if (_maskRight != null) _maskRight.pickingMode = PickingMode.Position;
            if (_skipButton != null) _skipButton.pickingMode = PickingMode.Position;

            if (_skipButton != null) _skipButton.clicked += OnSkipButtonTriggered;

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

        private void OnCapturedPointerDown(PointerDownEvent evt)
        {
            if (_barrier == null || _barrier.style.display == DisplayStyle.None) return;
            if (_skipButton != null && _skipButton.worldBound.Contains(evt.position)) return;

            if (!IsValidClickRequirement(evt.button))
            {
                evt.StopPropagation();
            }
        }

        private void OnCapturedPointerUp(PointerUpEvent evt)
        {
            if (_barrier == null || _barrier.style.display == DisplayStyle.None) return;
            if (_skipButton != null && _skipButton.worldBound.Contains(evt.position)) return;

            if (!IsValidClickRequirement(evt.button))
            {
                evt.StopPropagation();
            }
        }

        private bool IsValidClickRequirement(int mouseButton)
        {
            if (mouseButton != 0 && mouseButton != 1) return false;

            TutorialClickType inputClickType = mouseButton == 0 ? TutorialClickType.LeftClick : TutorialClickType.RightClick;
            TutorialClickType requiredClickType = _currentStepData != null ? _currentStepData.ClickType : TutorialClickType.AnyClick;

            if (requiredClickType != TutorialClickType.AnyClick && inputClickType != requiredClickType)
            {
                return false; 
            }
            return true; 
        }

        private void HandleUniversalInputTracking()
        {
            if (_isStepPendingAdvance) return;

            Mouse mouse = Mouse.current;
            if (mouse == null) return;
            
            // 🔒 잘못된 버튼을 누르거나 유지 중일 때는 배리어가 무조건 마우스를 막아 상호작용 차단
            if (_currentStepData != null && _currentStepData.ClickType != TutorialClickType.AnyClick)
            {
                if (_currentStepData.ClickType == TutorialClickType.LeftClick && mouse.rightButton.isPressed)
                {
                    _barrier.pickingMode = PickingMode.Position;
                    return;
                }
                if (_currentStepData.ClickType == TutorialClickType.RightClick && mouse.leftButton.isPressed)
                {
                    _barrier.pickingMode = PickingMode.Position;
                    return;
                }
            }

            // 마우스 포인터의 현재 위치 계산
            Vector2 mo = mouse.position.ReadValue();
            Vector2 lo = new Vector2(mo.x, Screen.height - mo.y);

            // 타겟 영역 안에 마우스가 들어가 있을 때만 하부 UI가 클릭되도록 pickingMode를 열어줍니다.
            if (_calculatedBounds.Contains(lo))
            {
                _barrier.pickingMode = PickingMode.Ignore; // 클릭 허용
            }
            else
            {
                _barrier.pickingMode = PickingMode.Position; // 클릭 차단
            }

            bool leftReleased = mouse.leftButton.wasReleasedThisFrame;
            bool rightReleased = mouse.rightButton.wasReleasedThisFrame;

            if (leftReleased || rightReleased)
            {
                int currentButton = leftReleased ? 0 : 1;
                
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

            // 💥 점(.)대신 쉼표(,)를 구분자로 사용하므로 월드 오브젝트 체크 조건도 변경합니다.
            _isTrackingWorld = !string.IsNullOrEmpty(_targetKey) && !_targetKey.Contains(",") && GameObject.Find(_targetKey) != null;

            if (_guideLabel != null) _guideLabel.text = $"({(stepData.ClickType == (TutorialClickType)1 ? "우클릭" : "클릭")}) \n{stepData.Description}";
        }

        // 🔄 이름과 클래스를 모두 유연하게 찾아주는 핵심 리팩토링 구역
        private VisualElement FindTargetVisualElement(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            // /를 구분 기호로 사용하여 split을 수행합니다.
            string[] split = key.Split('/');

            // 1. 계층형 경로 검색 (예: TownUi, embark-overlay, .btn-style)
            if (split.Length > 1)
            {
                // 첫 번째 요소는 무조건 템플릿을 소유한 GameObject 이름 (클래스일 수 없으므로 Trim만 처리)
                GameObject docGo = GameObject.Find(split[0].Trim());
                if (docGo != null && docGo.TryGetComponent<UIDocument>(out var specDoc))
                {
                    VisualElement current = specDoc.rootVisualElement;
                    if (current == null) return null;

                    for (int i = 1; i < split.Length; i++)
                    {
                        // 내부 헬퍼 메서드(QueryElement)를 통해 이름 혹은 클래스로 다음 분기를 검색합니다.
                        current = QueryElement(current, split[i].Trim());
                        if (current == null) return null; 
                    }
                    return current;
                }
            }
            // 2. 단일 키 검색 (백업용)
            else
            {
                var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                foreach (var doc in uiDocuments)
                {
                    if (doc != null && doc != townUiDocument && doc.rootVisualElement != null)
                    {
                        var el = QueryElement(doc.rootVisualElement, key.Trim());
                        if (el != null) return el;
                    }
                }
            }

            return null;
        }

        /// <summary>
        /// 점(.)의 유무를 판별하여 이름 또는 USS 클래스로 요소를 정밀 검색하는 헬퍼 메서드
        /// </summary>
        private VisualElement QueryElement(VisualElement root, string target)
        {
            if (root == null || string.IsNullOrEmpty(target)) return null;

            VisualElement result = null;
            int targetIndex = -1; // -1이면 인덱스 조건이 없음을 의미

            // 🎯 [인덱스 파싱] 주소 끝에 '[숫자]'가 붙어있는지 정밀 수동 파싱
            if (target.EndsWith("]"))
            {
                int openBracket = target.IndexOf('[');
                if (openBracket != -1)
                {
                    string indexStr = target.Substring(openBracket + 1, target.Length - openBracket - 2).Trim();
                    if (int.TryParse(indexStr, out int parsedIndex))
                    {
                        targetIndex = parsedIndex;
                        target = target.Substring(0, openBracket).Trim(); // 본래의 클래스명 또는 ID만 남김
                    }
                }
            }

            // 클래스 기반(.으로 시작) vs 이름 기반 분기 정의
            bool isClass = target.StartsWith(".");
            string searchKey = isClass ? target.Substring(1) : target;

            // 1. 1차 자식단 검색 시작
            if (isClass)
            {
                // 대괄호 인덱스가 없는 경우 -> 가장 먼저 발견되는 첫 번째 요소 반환
                if (targetIndex == -1)
                {
                    result = root.Q<VisualElement>(name: null, className: searchKey);
                    if (result == null && root is TemplateContainer template)
                    {
                        result = template.contentContainer.Q<VisualElement>(name: null, className: searchKey);
                    }
                }
                // 💥 인덱스가 지정된 경우 -> 조건에 맞는 n번째 요소 정밀 서칭 (범용성 최고)
                else
                {
                    List<VisualElement> candidates =
                        root.Query<VisualElement>(name: null, className: searchKey).ToList();
                    if (candidates.Count > targetIndex) result = candidates[targetIndex];
                    else if (candidates.Count > 0) result = candidates[0]; // 인덱스 초과 시 방어용 보정
                }
            }
            else
            {
                // 이름(ID) 기반 검색
                if (targetIndex == -1)
                {
                    result = root.Q<VisualElement>(searchKey);
                    if (result == null && root is TemplateContainer template)
                    {
                        result = template.contentContainer.Q<VisualElement>(searchKey);
                    }
                }
                else
                {
                    List<VisualElement> candidates = root.Query<VisualElement>(searchKey).ToList();
                    if (candidates.Count > targetIndex) result = candidates[targetIndex];
                    else if (candidates.Count > 0) result = candidates[0];
                }
            }

            // 2. 💥 형제 역추적 예외 보강 (자식단에서 찾지 못했을 때, 부모 레이어에서 n번째 리스트 재쿼리)
            if (result == null && root.parent != null)
            {
                if (isClass)
                {
                    if (targetIndex == -1)
                    {
                        result = root.parent.Q<VisualElement>(name: null, className: searchKey);
                    }
                    else
                    {
                        List<VisualElement> candidates =
                            root.parent.Query<VisualElement>(name: null, className: searchKey).ToList();
                        if (candidates.Count > targetIndex) result = candidates[targetIndex];
                    }
                }
                else
                {
                    if (targetIndex == -1)
                    {
                        result = root.parent.Q<VisualElement>(searchKey);
                    }
                    else
                    {
                        List<VisualElement> candidates = root.parent.Query<VisualElement>(searchKey).ToList();
                        if (candidates.Count > targetIndex) result = candidates[targetIndex];
                    }
                }
            }

            return result;
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