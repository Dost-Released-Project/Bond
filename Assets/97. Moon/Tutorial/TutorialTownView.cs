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

            // ─── [Start() 내부 변동점] ───
            if (_barrier != null)
            {
                _barrier.pickingMode = PickingMode.Position;
                // 기존 PointerDown 등록 (TrickleDown 유지)
                _barrier.RegisterCallback<PointerDownEvent>(OnBarrierPointerDown, TrickleDown.TrickleDown);
    
                // 💥 [추가] 마우스 휠 스크롤 신호를 하위 레이어로 100% 관통시키는 콜백 등록
                _barrier.RegisterCallback<WheelEvent>(OnBarrierWheel, TrickleDown.TrickleDown);
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

            // ─── 💥 [네임스페이스 계층형 UI 식별 엔진] ───
            VisualElement targetUi = null;
            string targetKey = stepData.TargetUiKey;

            // 케이스 A: 테이블에 "ConstructionUI.btn-confirm" 형태로 적혀있는 경우
            if (targetKey.Contains("."))
            {
                string[] split = targetKey.Split('.');
                string docGameObjectName = split[0].Trim();
                string uiElementName = split[1].Trim();

                GameObject docGo = GameObject.Find(docGameObjectName);
                if (docGo != null && docGo.TryGetComponent<UIDocument>(out var specDoc))
                {
                    if (specDoc.rootVisualElement != null)
                    {
                        targetUi = specDoc.rootVisualElement.Q<VisualElement>(uiElementName);
                    }
                }
            }
            // 케이스 B: 기존의 단일 명칭 고유 엘리먼트 검색 (예: resource-container)
            else
            {
                var uiDocuments = FindObjectsByType<UIDocument>(FindObjectsSortMode.None);
                foreach (var doc in uiDocuments)
                {
                    if (doc != null && doc.rootVisualElement != null && doc != townUiDocument)
                    {
                        targetUi = doc.rootVisualElement.Q<VisualElement>(targetKey);
                        if (targetUi != null) break;
                    }
                }
            }
            
            // ─── [OnStepUpdated 내부의 UI 스캔 파트 최종 정석 교정본] ───
            if (targetUi != null)
            {
                // 💥 [핵심 방어] 50ms 반복 타이머(Every)를 완전히 철거합니다. (깜빡임 버그의 원인)
                // 대신, 타겟 엘리먼트가 속한 UIDocument의 최상위 계층에 포스트 레이아웃 이벤트를 걸어
                // 대원 명부 팝업창이 실제로 연출을 마치고 눈에 보이는 순간에 딱 1번만 정확하게 좌표를 낚아챕니다.
                targetUi.RegisterCallback<GeometryChangedEvent>(evt => 
                {
                    // UI Toolkit 엔진이 계산한 최종 절대 화면 픽셀 좌표 추출
                    Rect completeBounds = targetUi.worldBound;
                
                    // 아직 창이 열리는 중이라 크기가 0이거나 NaN이면 연산을 보류하여 자원바로 튀는 현상 방어
                    if (float.IsNaN(completeBounds.width) || completeBounds.width <= 0) return;

                    _currentFocusBounds = completeBounds;
                    UpdateMaskAndGuideLayout(completeBounds);
                });

                // 💥 [실시간 스케일 보정] 유저가 창을 열었을 때 첫 연산 오차를 방어하기 위해
                // UI Toolkit의 안전한 스케줄러를 딱 "1프레임 뒤"에만 1회성(Once)으로 실행하여 픽셀을 강제 흡착시킵니다.
                targetUi.schedule.Execute(() =>
                {
                    if (targetUi == null || _barrier == null || _barrier.style.display == DisplayStyle.None) return;

                    Rect stableBounds = targetUi.worldBound;
                    if (!float.IsNaN(stableBounds.width) && stableBounds.width > 0)
                    {
                        _currentFocusBounds = stableBounds;
                        UpdateMaskAndGuideLayout(stableBounds);
                    }
                });
            
                // 첫 프레임 선행 예외 방어 대입
                Rect directBounds = targetUi.worldBound;
                if (!float.IsNaN(directBounds.width) && directBounds.width > 0)
                {
                    _currentFocusBounds = directBounds;
                    UpdateMaskAndGuideLayout(directBounds);
                }
            }
            else
            {
                // 3. UI가 아니라면 월드 오브젝트(Construction_Slot02 등)로 판정하고 프레임 추적 락 활성화
                _isTrackingWorldObject = true;
            }
        }

        // ─── [UpdateMaskAndGuideLayout 내부 텍스트 및 마스크 레이아웃 보정] ───
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

            // 4분할 마스크 패널 연산 유지
            if (_maskTop != null) { _maskTop.style.left = 0; _maskTop.style.top = 0; _maskTop.style.width = screenWidth; _maskTop.style.height = bounds.y; }
            if (_maskBottom != null) { _maskBottom.style.left = 0; _maskBottom.style.top = bounds.y + bounds.height; _maskBottom.style.width = screenWidth; _maskBottom.style.height = screenHeight - (bounds.y + bounds.height); }
            if (_maskLeft != null) { _maskLeft.style.left = 0; _maskLeft.style.top = bounds.y; _maskLeft.style.width = bounds.x; _maskLeft.style.height = bounds.height; }
            if (_maskRight != null) { _maskRight.style.left = bounds.x + bounds.width; _maskRight.style.top = bounds.y; _maskRight.style.width = screenWidth - (bounds.x + bounds.width); _maskRight.style.height = bounds.height; }

            if (_guideLabel != null)
            {
                _guideLabel.style.position = Position.Absolute;
                _guideLabel.style.width = 450;

                // 💥 닫기 버튼이 화면 가장자리(우측 상단)에 바짝 붙어 있으므로, 
                // 글자 박스가 화면 오른쪽 밖으로 튀어나가지 않도록 우측 마진 가이드라인 폭을 Clamp로 타이트하게 조입니다.
                float targetLeft = bounds.x + (bounds.width / 2f) - 225f;
                _guideLabel.style.left = Mathf.Clamp(targetLeft, 30f, screenWidth - 480f);

                // 닫기 창 바로 밑에 이쁘게 안착하도록 세로축 패딩 매핑
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

        private void OnBarrierPointerDown(PointerDownEvent evt)
        {
            Vector2 clickPosition = evt.localPosition;
            
            // 💥 [추가] 유저가 누른 위치가 스킵 버튼의 물리적 화면 영역(worldBound) 내부라면,
            // 배리어의 모든 튜토리얼 락을 완전히 바이패스(Pass)하여 스킵 버튼 고유의 클릭 이벤트가 실행되도록 즉시 리턴합니다.
            if (_skipButton != null && _skipButton.worldBound.Contains(clickPosition))
            {
                Debug.Log("<color=orange>[Tutorial Bypass]</color> 스킵 버튼 영역 클릭 감지. 튜토리얼 차단막을 우회합니다.");
                return; 
            }

            // 🎯 마우스 클릭 위치가 현재 활성화된 노란색 포커스 영역 내부인지 검사
            if (_currentFocusBounds.Contains(clickPosition))
            {
                // 1. 순간적으로 배리어를 화면에서 숨겨 레이캐스트 판정을 유니티 아래 레이어로 완전 개방
                _barrier.style.display = DisplayStyle.None;

                // 2. UI Toolkit 패널 하위 진짜 원소 강제 클릭 주입 (UI 요소 대응)
                var targetPanel = townUiDocument.rootVisualElement.panel;
                VisualElement clickedElement = targetPanel?.Pick(clickPosition);
                if (clickedElement != null)
                {
                    if (evt.button == 0) // 좌클릭
                    {
                        using (NavigationSubmitEvent submitEvt = NavigationSubmitEvent.GetPooled())
                        {
                            submitEvt.target = clickedElement;
                            clickedElement.SendEvent(submitEvt);
                        }
                    }
                    else if (evt.button == 1) // 우클릭
                    {
                        using (PointerDownEvent rightClickEvt = PointerDownEvent.GetPooled(evt))
                        {
                            rightClickEvt.target = clickedElement;
                            clickedElement.SendEvent(rightClickEvt);
                        }
                    }
                }

                // 3. 💥 [핵심 보정] 레거시 마우스 및 월드 스페이스 기물 정밀 추적 연동
                Ray ray = mainCamera.ScreenPointToRay(new Vector3(clickPosition.x, Screen.height - clickPosition.y, 0));
                if (Physics.Raycast(ray, out RaycastHit hit))
                {
                    GameObject hitGo = hit.collider.gameObject;
                    Debug.Log($"<color=cyan>[Tutorial Raycast]</color> 충돌 오브젝트 탐색: {hitGo.name} | 마우스 버튼: {evt.button}");

                    // ─── 분기 A: 아직 비어있는 빈 건설 슬롯을 클릭한 경우 ───
                    if (hitGo.TryGetComponent<ConstructionSlot>(out var slot))
                    {
                        var constructionUi = FindAnyObjectByType<ConstructionUI>();
                        if (constructionUi != null)
                        {
                            Debug.Log($"<color=lime>[Tutorial UI Bridge]</color> 빈 슬롯 건축 확인 UI 오픈 집행.");
                            
                            // 튜토리얼 배리어 복구 및 인게임 진짜 건축 UI 데이터 주입 호출
                            _barrier.style.display = DisplayStyle.Flex;
                            constructionUi.OpenConstruction(slot.slotIndex, slot.AllowableType);
                            
                            // 튜토리얼 상태 단계 전진
                            _controller.Advance();
                            return; // 처리 완공 후 즉시 탈출하여 중복 실행 방지
                        }
                    }
                    // ─── 분기 B: 건물이 완공된 후 해당 기물을 클릭한 경우 ───
                    else if (hitGo.GetComponentInChildren<BuildingObject>() != null || hitGo.GetComponentInParent<BuildingObject>() != null)
                    {
                        var bObj = hitGo.GetComponentInChildren<BuildingObject>() ?? hitGo.GetComponentInParent<BuildingObject>();
                        var constructionUi = FindAnyObjectByType<ConstructionUI>();

                        if (bObj != null && constructionUi != null)
                        {
                            _barrier.style.display = DisplayStyle.Flex;

                            if (evt.button == 0) // 🏡 [좌클릭]: 보급소 기능 이용 및 효과 사용 팝업 오픈
                            {
                                Debug.Log($"<color=lime>[Tutorial UI Bridge]</color> 완공 건물 좌클릭 -> 기능 이용 UI 오픈.");
                                constructionUi.OpenInteraction(bObj);
                            }
                            else if (evt.button == 1) // 📈 [우클릭]: 건물 업그레이드 전용 팝업 오픈
                            {
                                Debug.Log($"<color=lime>[Tutorial UI Bridge]</color> 완공 건물 우클릭 -> 업그레이드 UI 오픈.");
                                constructionUi.OpenUpgrade(bObj);
                            }

                            _controller.Advance();
                            return;
                        }
                    }
                    else
                    {
                        // 기타 일반 기물 레거시 컴포넌트 마우스 메시지 수신 패싱 보존
                        if (evt.button == 0) hitGo.SendMessage("OnMouseDown", SendMessageOptions.DontRequireReceiver);
                        else if (evt.button == 1) hitGo.SendMessage("OnMouseOver", SendMessageOptions.DontRequireReceiver);
                    }
                }

                // 4. 입력 관통 처리가 끝난 후 안전하게 배리어 가림막 복구
                _barrier.style.display = DisplayStyle.Flex;
                
                // 일반 UI 요소 클릭 시에도 단계를 정상적으로 밀어 올림
                if (evt.button == 0 || evt.button == 1) 
                {
                    _controller.Advance();
                }
            }
            else
            {
                evt.StopPropagation(); // 노란색 영역 바깥 오클릭 철저 차단
            }
        }
        
        // ─── [신규 전용 메소드 추가] ───
        /// <summary>
        /// 마우스 휠 스크롤 신호를 배리어 아래 레이어로 100% 관통시킵니다.
        /// </summary>
        private void OnBarrierWheel(WheelEvent evt)
        {
            // 배리어를 잠시 꺼서 유니티 엔진 및 하위 UI가 스크롤 입력을 온전히 수신하게 만듭니다.
            _barrier.pickingMode = PickingMode.Ignore;

            // 현재 휠 이벤트 신호의 전파 차단을 방지하여 버블링 허용
            // (이 처리가 끝나면 마우스 무빙에 의해 뷰의 Update 루프가 마스크 좌표를 계속 자동 동기화합니다.)
    
            // 다음 프레임에 배리어 pickingMode를 다시 Position으로 복구하기 위해 타이머 유도
            _barrier.schedule.Execute(() => 
            {
                if (_barrier != null) _barrier.pickingMode = PickingMode.Position;
            }).ExecuteLater(10);
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