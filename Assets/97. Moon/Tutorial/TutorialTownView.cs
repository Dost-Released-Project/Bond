using UnityEngine;
using UnityEngine.UIElements;
using VContainer;

namespace Bond.Tutorial
{
    public class TutorialTownView : MonoBehaviour
    {
        private TutorialSystemController _controller;
        private SettlementManager _settlementManager;

        [SerializeField] private UIDocument townUiDocument;
        [Header("카메라 (월드 좌표 환산용)")]
        [SerializeField] private Camera mainCamera;
        
        private VisualElement _root;
        private VisualElement _barrier;
        private VisualElement _focusBox;
        private Label _guideLabel;
        private Button _skipButton;

        // 4분할 마스크 패널 레퍼런스
        private VisualElement _maskTop;
        private VisualElement _maskBottom;
        private VisualElement _maskLeft;
        private VisualElement _maskRight;

        private Rect _currentFocusBounds;
        private string _currentTargetUiKey;
        private bool _isTrackingWorldObject;

        [Inject]
        public void Construct(TutorialSystemController controller, SettlementManager settlementManager)
        {
            _controller = controller;
            _settlementManager = settlementManager;
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

            if (_skipButton != null)
            {
                _skipButton.clicked += OnSkipButtonTriggered;
                _skipButton.style.position = Position.Absolute;
                _skipButton.style.right = 40;
                _skipButton.style.bottom = 40;
            }

            if (_barrier != null)
            {
                _barrier.pickingMode = PickingMode.Position;
                _barrier.RegisterCallback<PointerDownEvent>(OnBarrierPointerDown, TrickleDown.TrickleDown);
            }

            if (_focusBox != null) _focusBox.pickingMode = PickingMode.Ignore;

            // 💥 [글자 잘림 원천 방어 스타일 강제 주입]
            if (_guideLabel != null)
            {
                _guideLabel.style.whiteSpace = WhiteSpace.Normal; // 자동 줄바꿈 활성화
                _guideLabel.style.flexShrink = 0; // 박스 축소 방지
                _guideLabel.style.color = Color.white;
                _guideLabel.style.backgroundColor = new Color(0, 0, 0, 0.85f);
                _guideLabel.style.paddingLeft = 12;
                _guideLabel.style.paddingRight = 12;
                _guideLabel.style.paddingTop = 12;
                _guideLabel.style.paddingBottom = 12;
                _guideLabel.style.borderTopLeftRadius = 6;
                _guideLabel.style.borderTopRightRadius = 6;
                _guideLabel.style.borderBottomLeftRadius = 6;
                _guideLabel.style.borderBottomRightRadius = 6;
            }

            _controller.OnStepChanged += OnStepUpdated;
            _controller.OnTutorialFinished += OnTutorialCleared;

            _controller.LoadProgress();
        }

        // 💥 [실시간 동적 추적 엔진] 카메라 이동/애니메이션 중에도 월드 기물 좌표를 실시간 동기화
        private void Update()
        {
            if (_isTrackingWorldObject && !string.IsNullOrEmpty(_currentTargetUiKey) && _barrier.style.display == DisplayStyle.Flex)
            {
                GameObject targetObj = GameObject.Find(_currentTargetUiKey);
                if (targetObj != null)
                {
                    Rect worldToScreenBounds = CalculateWorldBoundsToScreen(targetObj);
                    _currentFocusBounds = worldToScreenBounds;
                    UpdateMaskAndGuideLayout(worldToScreenBounds);
                }
            }
        }

        private void OnStepUpdated(TutorialStepSO stepData)
        {
            if (stepData.Sequence != TutorialSequence.Sequence_A_Town)
            {
                if (_barrier != null) _barrier.style.display = DisplayStyle.None;
                return;
            }

            if (_barrier == null || _focusBox == null) return;

            _barrier.style.display = DisplayStyle.Flex;
            _currentTargetUiKey = stepData.TargetUiKey;
            _isTrackingWorldObject = false;

            if (_guideLabel != null) _guideLabel.text = stepData.Description;

            // 1. 전역 UI 스캔
            VisualElement targetUi = null;
            var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
            foreach (var doc in uiDocuments)
            {
                if (doc != null && doc.rootVisualElement != null && doc != townUiDocument)
                {
                    targetUi = doc.rootVisualElement.Q<VisualElement>(stepData.TargetUiKey);
                    if (targetUi != null) break;
                }
            }
            
            if (targetUi != null)
            {
                targetUi.RegisterCallback<GeometryChangedEvent>(evt => 
                {
                    Rect globalBounds = targetUi.worldBound;
                    if (!float.IsNaN(globalBounds.width) && !float.IsNaN(globalBounds.height))
                    {
                        _currentFocusBounds = globalBounds;
                        UpdateMaskAndGuideLayout(globalBounds);
                    }
                });

                Rect directBounds = targetUi.worldBound;
                if (!float.IsNaN(directBounds.width) && !float.IsNaN(directBounds.height))
                {
                    _currentFocusBounds = directBounds;
                    UpdateMaskAndGuideLayout(directBounds);
                }
            }
            else
            {
                // 2. 월드 기물 상태 플래그 온 (Update에서 매 프레임 좌표 자동 갱신 추적)
                _isTrackingWorldObject = true;
            }
        }

        private void UpdateMaskAndGuideLayout(Rect bounds)
        {
            float screenWidth = Screen.width;
            float screenHeight = Screen.height;

            if (_focusBox != null)
            {
                _focusBox.style.position = Position.Absolute;
                _focusBox.style.left = bounds.x;
                _focusBox.style.top = bounds.y;
                _focusBox.style.width = bounds.width;
                _focusBox.style.height = bounds.height;
            }

            // 4분할 레이아웃 동적 갱신
            if (_maskTop != null) { _maskTop.style.left = 0; _maskTop.style.top = 0; _maskTop.style.width = screenWidth; _maskTop.style.height = bounds.y; }
            if (_maskBottom != null) { _maskBottom.style.left = 0; _maskBottom.style.top = bounds.y + bounds.height; _maskBottom.style.width = screenWidth; _maskBottom.style.height = screenHeight - (bounds.y + bounds.height); }
            if (_maskLeft != null) { _maskLeft.style.left = 0; _maskLeft.style.top = bounds.y; _maskLeft.style.width = bounds.x; _maskLeft.style.height = bounds.height; }
            if (_maskRight != null) { _maskRight.style.left = bounds.x + bounds.width; _maskRight.style.top = bounds.y; _maskRight.style.width = screenWidth - (bounds.x + bounds.width); _maskRight.style.height = bounds.height; }

            // 텍스트 스마트 가변 배치 및 줄바꿈 공간 확보
            if (_guideLabel != null)
            {
                _guideLabel.style.position = Position.Absolute;
                _guideLabel.style.width = 450; // 가로 영역 대폭 확장하여 줄바꿈 안정화

                float targetLeft = bounds.x + (bounds.width / 2f) - 225f;
                _guideLabel.style.left = Mathf.Clamp(targetLeft, 30f, screenWidth - 480f);

                if (bounds.y + bounds.height + 140f > screenHeight)
                {
                    _guideLabel.style.top = Mathf.Max(10f, bounds.y - 110f);
                }
                else
                {
                    _guideLabel.style.top = bounds.y + bounds.height + 20f;
                }
            }
        }

        // 💥 [클릭 가로채기 심층 해결] 락 해제 후 완벽한 패스스루 시뮬레이션
        private void OnBarrierPointerDown(PointerDownEvent evt)
        {
            Vector2 clickPosition = evt.localPosition;

            if (_currentFocusBounds.Contains(clickPosition))
            {
                // 1. 순간적으로 배리어를 화면에서 숨겨 입력 판정을 유니티 아래 레이어로 완전 개방
                _barrier.style.display = DisplayStyle.None;

                // 2. UI Toolkit 패널에서 실제 위치한 하위 원소를 정확히 검출하여 강제 클릭 버블링 실행
                var targetPanel = townUiDocument.rootVisualElement.panel;
                VisualElement clickedElement = targetPanel?.Pick(clickPosition);
                if (clickedElement != null)
                {
                    using (NavigationSubmitEvent submitEvt = NavigationSubmitEvent.GetPooled())
                    {
                        submitEvt.target = clickedElement;
                        clickedElement.SendEvent(submitEvt);
                    }
                }

                // 3. 💥 [최종 기획 매핑] 데이터 주입형 인게임 건축 UI 창 정밀 타격 오픈
                Ray ray = mainCamera.ScreenPointToRay(new Vector3(clickPosition.x, Screen.height - clickPosition.y, 0));
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    GameObject hitGo = hit.collider.gameObject;
                    Debug.Log($"<color=cyan>[Tutorial Raycast]</color> 충돌 오브젝트 탐색 성공: {hitGo.name}");

                    // Case A: 아직 비어있는 빈 건설 슬롯을 클릭한 경우
                    if (hitGo.TryGetComponent<ConstructionSlot>(out var slot))
                    {
                        var constructionUi = FindAnyObjectByType<ConstructionUI>();
                        if (constructionUi != null)
                        {
                            Debug.Log($"<color=lime>[Tutorial UI Bridge]</color> 슬롯 {slot.slotIndex}에 지정된 {slot.AllowableType} 데이터를 주입하며 OpenConstruction을 호출합니다.");
                            
                            // 💥 [핵심 보정] 튜토리얼 단계를 다음 텍스트 스텝으로 전진시킨 뒤 배리어 복구
                            _controller.Advance();
                            _barrier.style.display = DisplayStyle.Flex;

                            // 💥 단순히 Show만 켜던 오류를 완벽히 청쇄하고, 슬롯 정보와 허용 건물 유형을 100% 동기화 전달합니다.
                            constructionUi.OpenConstruction(slot.slotIndex, slot.AllowableType);
                            return; 
                        }
                    }
                    // Case B: 이미 완공된 건물을 클릭해서 상호작용/보급 UI를 띄우게 유도하는 분기
                    else if (hitGo.GetComponentInChildren<BuildingObject>() != null || hitGo.GetComponentInParent<BuildingObject>() != null)
                    {
                        var bObj = hitGo.GetComponentInChildren<BuildingObject>() ?? hitGo.GetComponentInParent<BuildingObject>();
                        if (bObj != null)
                        {
                            _controller.Advance();
                            _barrier.style.display = DisplayStyle.Flex;
                            
                            var settlementMgr = FindAnyObjectByType<SettlementManager>();
                            if (settlementMgr != null)
                            {
                                // 원래 영지 매니저의 기능 클릭 창을 정상적으로 연동 실행합니다.
                                settlementMgr.OnBuildingClicked(bObj);
                            }
                            return;
                        }
                    }
                }

                // 4. 입력 관통 처리가 완료된 후 즉시 배리어 마스크 복구
                _barrier.style.display = DisplayStyle.Flex;
                
                // 5. 다음 튜토리얼 스텝으로 코어 상태 변경 전진
                _controller.Advance();
            }
            else
            {
                evt.StopPropagation(); // 구멍 밖 오클릭 차단
            }
        }

        private Rect CalculateWorldBoundsToScreen(GameObject targetGo)
        {
            Bounds objectBounds = new Bounds(targetGo.transform.position, Vector3.one * 1.5f);
            if (targetGo.TryGetComponent<Collider>(out var collider)) objectBounds = collider.bounds;
            else if (targetGo.TryGetComponent<SpriteRenderer>(out var renderer)) objectBounds = renderer.bounds;

            Vector3 screenMin = mainCamera.WorldToScreenPoint(objectBounds.min);
            Vector3 screenMax = mainCamera.WorldToScreenPoint(objectBounds.max);

            float x = screenMin.x;
            float y = Screen.height - screenMax.y; 
            float width = screenMax.x - screenMin.x;
            float height = screenMax.y - screenMin.y;

            return new Rect(x, y, width, height);
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
        }

        private void OnDestroy()
        {
            if (_controller != null)
            {
                _controller.OnStepChanged -= OnStepUpdated;
                _controller.OnTutorialFinished -= OnTutorialCleared;
            }
        }
    }
}