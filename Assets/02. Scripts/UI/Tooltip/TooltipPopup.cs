using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.UI
{
    /// <summary>
    /// 범용 툴팁 표시 엔진. "어디에 / 어떻게 띄울지"(부착·실측·flip·경계 clamp·표시·숨김)만 담당한다.
    /// 툴팁의 생김새(콘텐츠)는 호출부가 만든 <see cref="VisualElement"/>를 그대로 받아 표시하므로,
    /// 이 엔진은 스타일에 관여하지 않는다(스킬/건물 툴팁처럼 자기만의 리치 레이아웃을 그대로 넘겨도 됨).<para/>
    /// 
    /// 핵심 동작:<br/>
    /// - 콘텐츠를 anchor가 속한 "문서 루트(UIDocument 루트)"에 부착 → 문서 USS 스코프 안이라 클래스 스타일 정상 적용.<br/>
    /// - 실제 레이아웃을 측정한 뒤(고정 추정 X) 패널 경계 안으로 clamp, 아래 공간이 없으면 위로 flip.<br/>
    /// - 패널마다 재사용 레이어 1개를 패널 트리에 심어 두므로(정적 가변 상태 없음) 어느 패널/호출부에서도 사용 가능.<para/>
    ///
    /// 사용 예:
    /// <code>
    /// // 1) 요소 hover 시 자동 표시 (가장 흔한 형태)
    /// var handle = TooltipPopup.Attach(chip, () => "무기\n롱소드\nSTR +3");
    /// // ... Dispose 시 해제
    /// handle.Dispose();
    ///
    /// // 2) 리치 콘텐츠를 직접 넘겨 표시 (생김새는 호출부 소유)
    /// TooltipPopup.Show(myRichTooltipElement, anchorElement);
    ///
    /// // 3) 마우스 좌표 기준 (맵 위 자유 오브젝트 등)
    /// TooltipPopup.ShowAt(content, evt.mousePosition, root);
    /// TooltipPopup.Hide(root);
    /// </code>
    /// </summary>
    public static class TooltipPopup
    {
        /// <summary>세로 배치 선호. Auto = 아래 우선, 공간 없으면 위로 flip.</summary>
        public enum Placement { Auto, Below, Above }

        private const string LayerName = "__tooltip-layer";
        private const float Gap = 4f;     // anchor와 툴팁 사이 간격
        private const float Margin = 6f;  // 패널 경계 여백
        private const string DefaultStyleResource = "Bond_Tooltip"; // Resources/Bond_Tooltip.uss

        private static StyleSheet _defaultStyle;
        private static bool _defaultStyleLoaded;

        // 구독할 게 없을 때(잘못된 인자 등) 돌려주는 무동작 핸들(매번 할당 방지).
        private static readonly Subscription _empty = new Subscription(null);

        // ── 공개 API ─────────────────────────────────────────────────

        /// <summary>anchor 요소를 기준으로 content를 띄운다(아래 우선, 공간 없으면 위로 flip).</summary>
        public static void Show(VisualElement content, VisualElement anchor, Placement prefer = Placement.Auto)
        {
            if (content == null || anchor?.panel == null) return;

            var layer = GetOrCreateLayer(ResolveDocumentRoot(anchor));
            var st = (State)layer.userData;
            st.Anchor = anchor;
            st.AnchorRect = default;
            st.Prefer = prefer;
            st.Offset = Vector2.zero;

            Mount(layer, content);
            Reposition(layer); // 이미 보이는 상태에서 재호출된 경우 즉시 보정. 첫 표시는 GeometryChanged가 처리.
        }

        /// <summary>간단 텍스트 툴팁(기본 제안 스킨). 콘텐츠를 직접 만들 필요가 없을 때.</summary>
        public static void Show(string text, VisualElement anchor, Placement prefer = Placement.Auto)
        {
            if (string.IsNullOrEmpty(text) || anchor?.panel == null) return;
            EnsureDefaultStyle(anchor.panel);
            Show(BuildText(text), anchor, prefer);
        }

        /// <summary>임의 좌표(예: 마우스 위치, 패널 좌표계) 기준으로 content를 띄운다. context는 대상 문서 내 아무 요소나(문서 루트·패널 판별용).</summary>
        public static void ShowAt(VisualElement content, Vector2 panelPosition, VisualElement context, Placement prefer = Placement.Auto)
        {
            if (content == null || context?.panel == null) return;

            var layer = GetOrCreateLayer(ResolveDocumentRoot(context));
            var st = (State)layer.userData;
            st.Anchor = null;
            st.AnchorRect = new Rect(panelPosition.x, panelPosition.y, 0f, 0f);
            st.Prefer = prefer;
            st.Offset = new Vector2(12f, 2f); // 커서를 살짝 비켜 표시

            Mount(layer, content);
            Reposition(layer);
        }

        /// <summary>해당 패널의 (모든) 툴팁 레이어를 숨긴다. 동시에 보이는 건 하나뿐이라 사실상 그걸 숨김.</summary>
        public static void Hide(IPanel panel)
        {
            var root = panel?.visualTree;
            if (root == null) return;
            root.Query<VisualElement>(LayerName).ForEach(l => l.style.display = DisplayStyle.None);
        }

        /// <summary>요소가 속한 패널의 툴팁을 숨긴다.</summary>
        public static void Hide(VisualElement context) => Hide(context?.panel);

        /// <summary>
        /// target에 hover하면 provider()가 만든 콘텐츠를 자동 표시/숨김한다.
        /// 반환된 <see cref="IDisposable"/>을 Dispose하면 콜백을 해제한다(OnDestroy/Dispose에서 호출).
        /// </summary>
        public static IDisposable Attach(VisualElement target, Func<VisualElement> provider, Placement prefer = Placement.Auto)
        {
            if (target == null || provider == null) return _empty;

            EventCallback<MouseEnterEvent> onEnter = _ =>
            {
                var c = provider();
                if (c != null) Show(c, target, prefer);
            };
            EventCallback<MouseLeaveEvent> onLeave = _ => Hide(target);
            EventCallback<DetachFromPanelEvent> onDetach = _ => Hide(target);

            target.RegisterCallback(onEnter);
            target.RegisterCallback(onLeave);
            target.RegisterCallback(onDetach);

            return new Subscription(() =>
            {
                target.UnregisterCallback(onEnter);
                target.UnregisterCallback(onLeave);
                target.UnregisterCallback(onDetach);
                Hide(target);
            });
        }

        /// <summary>간단 텍스트 버전 <see cref="Attach(VisualElement, Func{VisualElement}, Placement)"/>.</summary>
        public static IDisposable Attach(VisualElement target, Func<string> textProvider, Placement prefer = Placement.Auto)
        {
            if (target == null || textProvider == null) return _empty;

            return Attach(target, () =>
            {
                var t = textProvider();
                if (string.IsNullOrEmpty(t)) return null;
                EnsureDefaultStyle(target.panel);
                return BuildText(t);
            }, prefer);
        }

        /// <summary>
        /// 기본 제안 스킨의 텍스트 라벨을 만든다. 색·테두리·폰트는 <c>.tooltip</c> USS(테마 변수)에서 온다.
        /// 스타일을 통일/교체하려면 <c>Resources/Bond_Tooltip.uss</c>만 수정하면 된다(엔진은 무관).
        /// </summary>
        public static Label BuildText(string text)
        {
            var label = new Label(text);
            label.AddToClassList("tooltip");
            // 구조 속성(색/폰트가 아닌 레이아웃)은 인라인 허용 — USS 미적용 환경에서도 줄바꿈은 보장.
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.maxWidth = 360f;
            return label;
        }

        // ── 내부 구현 ────────────────────────────────────────────────

        // anchor에서 위로 올라가 "패널 루트의 직속 자식"(= 문서 루트, UIDocument.rootVisualElement)을 찾는다.
        // 거기에 툴팁을 붙이면 문서의 <Style> USS 스코프 안이라 클래스 스타일이 정상 적용된다(투명 방지 + 호출부 클래스 덮어쓰기 가능).
        private static VisualElement ResolveDocumentRoot(VisualElement anchor)
        {
            var panelRoot = anchor.panel.visualTree;
            var host = anchor;
            while (host.parent != null && host.parent != panelRoot)
                host = host.parent;
            return host;
        }

        // host = 문서 루트. "패널마다"가 아니라 "문서마다" 레이어 1개를 그 문서 루트에 심는다.
        private static VisualElement GetOrCreateLayer(VisualElement host)
        {
            var layer = host.Q(LayerName);
            if (layer != null) return layer;

            layer = new VisualElement { name = LayerName };
            layer.style.position = Position.Absolute;
            layer.style.display = DisplayStyle.None;
            layer.pickingMode = PickingMode.Ignore; // 툴팁이 입력을 가로채지 않게
            layer.userData = new State();

            // 콘텐츠 크기가 확정되면(또는 위치가 변하면) 다시 clamp → "추정"이 아닌 "실측" 기반 배치.
            layer.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (layer.resolvedStyle.display != DisplayStyle.None)
                    Reposition(layer);
            });

            host.Add(layer);
            return layer;
        }

        private static void Mount(VisualElement layer, VisualElement content)
        {
            layer.Clear();
            SetPickingIgnore(content);
            layer.Add(content);

            layer.style.display = DisplayStyle.Flex;
            layer.style.visibility = Visibility.Hidden; // 실측·배치 전 깜빡임 방지
            layer.BringToFront();
        }

        private static void Reposition(VisualElement layer)
        {
            var parent = layer.parent;
            if (parent == null) return;

            float w = layer.layout.width;
            float h = layer.layout.height;
            if (w <= 0f || h <= 0f) return; // 아직 레이아웃 전 → 이어지는 GeometryChanged가 다시 호출

            var st = (State)layer.userData;
            Rect area = parent.worldBound;                                  // 문서 루트(보통 전체화면) = clamp 영역
            Rect a = st.Anchor != null ? st.Anchor.worldBound : st.AnchorRect;

            // 가로: anchor 좌측 정렬 후 경계로 clamp
            float left = a.xMin + st.Offset.x;
            left = Mathf.Clamp(left, area.xMin + Margin, Mathf.Max(area.xMin + Margin, area.xMax - w - Margin));

            // 세로: 아래 우선, 공간 부족하면 위로 flip, 마지막에 경계 clamp(안전망)
            float below = a.yMax + Gap + st.Offset.y;
            float above = a.yMin - Gap - h;
            bool fitsBelow = below + h <= area.yMax - Margin;
            bool fitsAbove = above >= area.yMin + Margin;
            float top = st.Prefer switch
            {
                Placement.Above => fitsAbove ? above : below,
                Placement.Below => fitsBelow ? below : above,
                _               => fitsBelow ? below : (fitsAbove ? above : below),
            };
            top = Mathf.Clamp(top, area.yMin + Margin, Mathf.Max(area.yMin + Margin, area.yMax - h - Margin));

            // 패널 좌표(world)를 부모 로컬로 변환해 left/top 지정
            Vector2 local = parent.WorldToLocal(new Vector2(left, top));
            layer.style.left = local.x;
            layer.style.top = local.y;
            layer.style.visibility = Visibility.Visible;
        }

        private static void SetPickingIgnore(VisualElement el)
        {
            el.pickingMode = PickingMode.Ignore;
            foreach (var child in el.Children())
                SetPickingIgnore(child);
        }

        private static void EnsureDefaultStyle(IPanel panel)
        {
            var root = panel?.visualTree;
            if (root == null) return;

            if (!_defaultStyleLoaded)
            {
                _defaultStyle = Resources.Load<StyleSheet>(DefaultStyleResource);
                _defaultStyleLoaded = true;
                if (_defaultStyle == null)
                    Debug.LogWarning($"<color=orange>[TooltipPopup]</color> 기본 스킨 '{DefaultStyleResource}.uss'를 Resources에서 찾지 못함. .tooltip 스타일 없이 표시됨.");
            }

            if (_defaultStyle != null && !root.styleSheets.Contains(_defaultStyle))
                root.styleSheets.Add(_defaultStyle);
        }

        private sealed class State
        {
            public VisualElement Anchor;   // 요소 기준일 때(매 배치 시 worldBound 재조회 → 레이아웃 변동 추종)
            public Rect AnchorRect;         // 좌표 기준일 때
            public Placement Prefer;
            public Vector2 Offset;
        }

        private sealed class Subscription : IDisposable
        {
            private Action _dispose;
            public Subscription(Action dispose) => _dispose = dispose;
            public void Dispose() { _dispose?.Invoke(); _dispose = null; }
        }
    }
}
