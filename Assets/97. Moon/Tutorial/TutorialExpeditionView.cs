using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.InputSystem; 
using System.Collections.Generic;
using VContainer;

namespace Bond.Tutorial
{
    public class TutorialExpeditionView : MonoBehaviour
    {
        private ExpeditionTutorialSystemController _controller;

        [SerializeField] private UIDocument expeditionUiDocument; 
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

        private string _targetKey;
        private Rect _calculatedBounds;
        private bool _isTrackingWorld;
        private bool _isStepPendingAdvance;
        private TutorialStepSO _currentStepData;

        [Inject]
        public void Construct(ExpeditionTutorialSystemController controller)
        {
            _controller = controller;
        }

        private void Start()
        {
            if (expeditionUiDocument == null) return;
            if (mainCamera == null) mainCamera = Camera.main;

            _root = expeditionUiDocument.rootVisualElement;
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
                // 💥 정밀 인덱스 파싱 기법 적용
                GameObject targetObj = FindIndexedGameObject(_targetKey);
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

        // 💥 [월드 게임 오브젝트 순서 제어용 핵심 탐색 엔진]
        private GameObject FindIndexedGameObject(string pathKey)
        {
            string cleanKey = pathKey.Trim();
            int targetIndex = 0;

            // 주소 끝에 '[숫자]'가 붙어있는지 정밀 수동 파싱
            if (cleanKey.EndsWith("]"))
            {
                int openBracket = cleanKey.LastIndexOf('[');
                if (openBracket != -1)
                {
                    string indexStr = cleanKey.Substring(openBracket + 1, cleanKey.Length - openBracket - 2);
                    if (int.TryParse(indexStr, out int parsedIndex))
                    {
                        targetIndex = parsedIndex;
                        cleanKey = cleanKey.Substring(0, openBracket).Trim(); // 이름만 추출
                    }
                }
            }

            // 1. 단일 이름 기반 검색 시도
            if (!cleanKey.Contains("/"))
            {
                // 겹치는 이름을 가진 월드의 모든 오브젝트 수집 (비활성화 상태 제외)
                GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                List<GameObject> matches = new List<GameObject>();
                
                foreach (var go in allObjects)
                {
                    if (go.name == cleanKey) matches.Add(go);
                }

                // 하이라키 순서대로 혹은 생성 순서의 무결성을 위해 인스턴스 ID나 이름을 기반으로 정렬 처리가 안전합니다.
                if (matches.Count > targetIndex) return matches[targetIndex];
                if (matches.Count > 0) return matches[0]; // 인덱스 초과 시 안전장치로 첫 번째 반환
                return null;
            }

            // 2. 경로형 기반 트리 추적 검색 (Canvas/MapPanel/MapParent/Map_Node_Button(Clone) 규칙 지원)
            string[] hierarchyParts = cleanKey.Split('/');
            GameObject currentGo = GameObject.Find(hierarchyParts[0].Trim());

            if (currentGo == null) return null;

            for (int i = 1; i < hierarchyParts.Length; i++)
            {
                string childName = hierarchyParts[i].Trim();
                Transform foundChild = null;

                // 마지막 노드이자 이름이 겹치는 레이어일 때 순서 처리
                if (i == hierarchyParts.Length - 1)
                {
                    int matchCount = 0;
                    for (int c = 0; c < currentGo.transform.childCount; c++)
                    {
                        Transform child = currentGo.transform.GetChild(c);
                        if (child.name == childName)
                        {
                            if (matchCount == targetIndex)
                            {
                                foundChild = child;
                                break;
                            }
                            matchCount++;
                        }
                    }
                    // 인덱스가 미스나면 마지막으로 발견된 같은 이름이라도 매칭 보정
                    if (foundChild == null) foundChild = currentGo.transform.Find(childName);
                }
                else
                {
                    foundChild = currentGo.transform.Find(childName);
                }

                if (foundChild == null) return null;
                currentGo = foundChild.gameObject;
            }

            return currentGo;
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

            Vector2 mo = mouse.position.ReadValue();
            Vector2 lo = new Vector2(mo.x, Screen.height - mo.y);

            if (_calculatedBounds.Contains(lo))
            {
                _barrier.pickingMode = PickingMode.Ignore; 
            }
            else
            {
                _barrier.pickingMode = PickingMode.Position; 
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

            if (stepData.Sequence != TutorialSequence.Sequence_B_Expedition)
            {
                if (_barrier != null) _barrier.style.display = DisplayStyle.None;
                return;
            }

            if (_barrier == null) return;

            _barrier.style.display = DisplayStyle.Flex;
            _targetKey = stepData.TargetUiKey;
            _calculatedBounds = new Rect(0, 0, 0, 0);
            _isStepPendingAdvance = false;

            // 월드 트래킹 여부 분석 판단 단어 확장 수정 ([...] 문법 우회용)
            string rootCheckKey = _targetKey.Contains("[") ? _targetKey.Substring(0, _targetKey.IndexOf('[')) : _targetKey;
            _isTrackingWorld = !string.IsNullOrEmpty(_targetKey) && !_targetKey.Contains("/") && (GameObject.Find(rootCheckKey.Trim()) != null || rootCheckKey.Contains("Canvas"));

            if (_guideLabel != null) _guideLabel.text = stepData.Description;
        }

        private VisualElement FindTargetVisualElement(string key)
        {
            if (string.IsNullOrEmpty(key)) return null;

            string[] split = key.Split('/');

            if (split.Length > 1)
            {
                GameObject docGo = GameObject.Find(split[0].Trim());
                if (docGo != null && docGo.TryGetComponent<UIDocument>(out var specDoc))
                {
                    VisualElement current = specDoc.rootVisualElement;
                    if (current == null) return null;

                    for (int i = 1; i < split.Length; i++)
                    {
                        current = QueryElement(current, split[i].Trim());
                        if (current == null) return null; 
                    }
                    return current;
                }
            }
            else
            {
                var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                foreach (var doc in uiDocuments)
                {
                    if (doc != null && doc != expeditionUiDocument && doc.rootVisualElement != null)
                    {
                        var el = QueryElement(doc.rootVisualElement, key.Trim());
                        if (el != null) return el;
                    }
                }
            }

            return null;
        }

        private VisualElement QueryElement(VisualElement root, string target)
        {
            if (root == null || string.IsNullOrEmpty(target)) return null;

            VisualElement result = null;

            if (target.StartsWith("."))
            {
                string className = target.Substring(1);
                result = root.Q<VisualElement>(name: null, className: className);
        
                if (result == null && root is TemplateContainer template)
                {
                    result = template.contentContainer.Q<VisualElement>(name: null, className: className);
                }
            }
            else
            {
                result = root.Q<VisualElement>(target);
        
                if (result == null && root is TemplateContainer template)
                {
                    result = template.contentContainer.Q<VisualElement>(target);
                }
            }

            if (result == null && root.parent != null)
            {
                if (target.StartsWith("."))
                {
                    string className = target.Substring(1);
                    result = root.parent.Q<VisualElement>(name: null, className: className);
                }
                else
                {
                    result = root.parent.Q<VisualElement>(target);
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
            // uGUI RectTransform 대응 최적화
            if (targetGo.TryGetComponent<RectTransform>(out var rectTransform))
            {
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);
                
                Vector3 minScreen = mainCamera.WorldToScreenPoint(corners[0]);
                Vector3 maxScreen = mainCamera.WorldToScreenPoint(corners[2]);

                float x = minScreen.x;
                float y = Screen.height - maxScreen.y;
                return new Rect(x, y, Mathf.Abs(maxScreen.x - minScreen.x), Mathf.Abs(maxScreen.y - minScreen.y));
            }

            Bounds objectBounds = new Bounds(targetGo.transform.position, Vector3.one * 1.5f);
            if (targetGo.TryGetComponent<Collider>(out var collider)) objectBounds = collider.bounds;
            else if (targetGo.TryGetComponent<SpriteRenderer>(out var renderer)) objectBounds = renderer.bounds;

            Vector3 minScreen2 = mainCamera.WorldToScreenPoint(objectBounds.min);
            Vector3 maxScreen2 = mainCamera.WorldToScreenPoint(objectBounds.max);

            float x2 = Mathf.Min(minScreen2.x, maxScreen2.x);
            float y2 = Screen.height - Mathf.Max(minScreen2.y, maxScreen2.y);
            return new Rect(x2, y2, Mathf.Abs(maxScreen2.x - minScreen2.x), Mathf.Abs(maxScreen2.y - minScreen2.y));
        }

        private void OnSkipButtonTriggered()
        {
            _controller.Skip(null, null);
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