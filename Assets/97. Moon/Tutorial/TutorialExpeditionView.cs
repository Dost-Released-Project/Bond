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
                GameObject targetObj = FindIndexedGameObject(_targetKey);
                if (targetObj != null)
                {
                    _calculatedBounds = CalculateWorldBoundsToScreen(targetObj);
                    
                    // 💥 [보정 및 안전장치] 데이터가 완전 제로거나 비정상적인 위치(동적 스폰 지연)라면 배리어를 잠시 열어 유저 락을 방지합니다.
                    if (_calculatedBounds.width > 0 && _calculatedBounds.height > 0)
                    {
                        ApplyLayout(_calculatedBounds);
                    }
                    else
                    {
                        _barrier.pickingMode = PickingMode.Ignore;
                    }
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

        private GameObject FindIndexedGameObject(string pathKey)
        {
            string cleanKey = pathKey.Trim();
            int targetIndex = 0;

            if (cleanKey.EndsWith("]"))
            {
                int openBracket = cleanKey.LastIndexOf('[');
                if (openBracket != -1)
                {
                    string indexStr = cleanKey.Substring(openBracket + 1, cleanKey.Length - openBracket - 2);
                    if (int.TryParse(indexStr, out int parsedIndex))
                    {
                        targetIndex = parsedIndex;
                        cleanKey = cleanKey.Substring(0, openBracket).Trim();
                    }
                }
            }

            if (!cleanKey.Contains("/"))
            {
                GameObject[] allObjects = GameObject.FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                List<GameObject> matches = new List<GameObject>();
                
                foreach (var go in allObjects)
                {
                    if (go.name == cleanKey) matches.Add(go);
                }

                if (matches.Count > targetIndex) return matches[targetIndex];
                if (matches.Count > 0) return matches[0]; 
                return null;
            }
            
            string[] hierarchyParts = cleanKey.Split('/');
            GameObject currentGo = null;

            if (hierarchyParts[0].Trim() == "Canvas")
            {
                GameObject[] canvases = FindObjectsByType<GameObject>(FindObjectsSortMode.None);
                foreach (var canvas in canvases)
                {
                    if (canvas.name == "Canvas" && canvas.activeInHierarchy)
                    {
                        if (hierarchyParts.Length > 1 && canvas.transform.Find(hierarchyParts[1].Trim()) != null)
                        {
                            currentGo = canvas;
                            break;
                        }
                    }
                }
            }

            if (currentGo == null)
            {
                currentGo = GameObject.Find(hierarchyParts[0].Trim());
            }

            if (currentGo == null) return null;

            for (int i = 1; i < hierarchyParts.Length; i++)
            {
                string childName = hierarchyParts[i].Trim();
                Transform foundChild = null;

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
            
            _isTrackingWorld = !string.IsNullOrEmpty(_targetKey) && 
                               (!_targetKey.Contains("/") || _targetKey.StartsWith("Canvas") || _targetKey.Contains("Canvas/"));

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
            // 🎯 uGUI RectTransform 스크린 실측 좌표 정산 엔진 (카메라/오버레이 전천후 대응)
            if (targetGo.TryGetComponent<RectTransform>(out var rectTransform))
            {
                Canvas rootCanvas = rectTransform.GetComponentInParent<Canvas>();

                // 💥 중요: 캔버스가 ScreenSpace-Overlay 모드라면 카메라를 무조건 null로 주어야 하고,
                // Camera 모드라면 해당 Canvas를 비추고 있는 정확한 Event Camera(혹은 mainCamera)를 매핑해야 꼬이지 않습니다.
                Camera cam = null;
                if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
                {
                    cam = rootCanvas.worldCamera != null ? rootCanvas.worldCamera : mainCamera;
                }

                // 월드 공간상의 4개 모서리 좌표를 획득합니다.
                Vector3[] corners = new Vector3[4];
                rectTransform.GetWorldCorners(corners);

                // 각 모서리를 카메라와 해상도 스케일에 맞춰 순수 스크린 픽셀 좌표로 변환합니다.
                Vector2 screenCorner0 = RectTransformUtility.WorldToScreenPoint(cam, corners[0]); // 좌하단
                Vector2 screenCorner2 = RectTransformUtility.WorldToScreenPoint(cam, corners[2]); // 우상단

                // UI Toolkit 마스킹 시스템과 1:1 매칭되는 물리적 픽셀 크기 산출
                float width = Mathf.Abs(screenCorner2.x - screenCorner0.x);
                float height = Mathf.Abs(screenCorner2.y - screenCorner0.y);

                // UI Toolkit의 원점 규칙(좌측 상단이 0,0)에 맞춰 정확하게 Pivot 오차를 보정합니다.
                float x = screenCorner0.x;
                float y = Screen.height - screenCorner2.y;

                // 🔍 디버깅용 실측 로그 추가
                Debug.Log(
                    $"<color=lime>[uGUI Coordinate Custom]</color> 오브젝트: {targetGo.name} | CanvasMode: {rootCanvas?.renderMode} | 적용된 카메라: {(cam != null ? cam.name : "Null(Overlay)")} | 최종 Rect: {x}, {y}, {width}, {height}");

                return new Rect(x, y, width, height);
            }

            // 3D/2D 일반 게임오브젝트용 기존 로직 유지
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
            _controller.Skip();
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