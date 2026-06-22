using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace Bond.UI
{
    /// <summary>
    /// 리액션 슬롯의 헤더/편집칸이 여는 떠있는 드롭다운 오버레이.
    /// 문서 루트에 절대배치로 붙어 슬롯 리스트를 밀지 않는다(인라인 풀과 달리 레이아웃 비파괴).
    /// 항목 내용(아이콘·텍스트·툴팁)은 호출부가 builder 로 채운다.<para/>
    /// 배치 규칙은 <see cref="TooltipPopup"/> 과 동일(앵커 아래 우선·공간 없으면 위로 flip·경계 clamp)하지만,
    /// 항목이 클릭 가능해야 하므로 picking 을 살린 별도 레이어를 쓴다.
    /// </summary>
    public class SlotDropdown : IDisposable
    {
        private const float Gap = 3f;
        private const float Margin = 6f;

        private readonly VisualElement _root;   // 문서 루트 — 오버레이 부착 + 바깥클릭 감지
        private readonly VisualElement _layer;   // 위치/프레임(테두리·배경·max-height·clip)
        private readonly ScrollView   _content;  // 항목 컨테이너 — 길어지면 세로 스크롤(찌그러짐 방지)

        /// <summary>현재 열려 있는 기준 앵커. 닫혀 있으면 null. (토글 판정용)</summary>
        public VisualElement CurrentAnchor { get; private set; }
        public bool IsOpen => CurrentAnchor != null;

        public SlotDropdown(VisualElement documentRoot)
        {
            _root = documentRoot;

            _layer = new VisualElement { name = "__slot-dropdown" };
            _layer.AddToClassList("char-detail__dropdown");
            _layer.style.position = Position.Absolute;
            _layer.style.display = DisplayStyle.None;

            // 항목은 스크롤 영역 안에 쌓는다 — 목록이 길어도 항목이 세로로 찌그러지지 않고(항목 flex-shrink:0) 넘치면 스크롤.
            _content = new ScrollView(ScrollViewMode.Vertical);
            _content.AddToClassList("char-detail__dropdown-scroll");
            _content.horizontalScrollerVisibility = ScrollerVisibility.Hidden;
            _content.verticalScrollerVisibility   = ScrollerVisibility.Auto;
            _layer.Add(_content);

            // 항목 크기가 확정(또는 변동)되면 실측 기반으로 다시 clamp.
            _layer.RegisterCallback<GeometryChangedEvent>(_ =>
            {
                if (IsOpen && _layer.resolvedStyle.display != DisplayStyle.None)
                    Reposition();
            });

            _root.Add(_layer);
            _root.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
        }

        /// <summary>앵커 기준으로 열고, builder 로 항목을 채운다. 같은 앵커 재호출 시에도 새로 채운다.</summary>
        public void Open(VisualElement anchor, Action<VisualElement> buildItems)
        {
            if (anchor == null) return;
            CurrentAnchor = anchor;

            _content.Clear();
            buildItems?.Invoke(_content);
            _content.scrollOffset = Vector2.zero;   // 새로 열 때 항상 맨 위부터

            _layer.style.display = DisplayStyle.Flex;
            _layer.style.visibility = Visibility.Hidden; // 실측·배치 전 깜빡임 방지
            _layer.BringToFront();
            Reposition();
        }

        public void Close()
        {
            CurrentAnchor = null;
            _content.Clear();
            _layer.style.display = DisplayStyle.None;
        }

        // 레이어/앵커 바깥을 누르면 닫는다. 앵커 자체 클릭은 호출부 토글에 맡긴다(여기선 유지).
        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (!IsOpen) return;
            var t = evt.target as VisualElement;
            if (t != null && (t == _layer || _layer.Contains(t) ||
                              (CurrentAnchor != null && (t == CurrentAnchor || CurrentAnchor.Contains(t)))))
                return;
            Close();
        }

        private void Reposition()
        {
            if (!IsOpen) return;
            float w = _layer.layout.width;
            float h = _layer.layout.height;
            if (w <= 0f || h <= 0f) return; // 레이아웃 전 → 이어지는 GeometryChanged 가 재호출

            Rect area = _root.worldBound;
            Rect a = CurrentAnchor.worldBound;

            // 가로: 앵커 좌측 정렬 후 경계 clamp
            float left = Mathf.Clamp(a.xMin, area.xMin + Margin, Mathf.Max(area.xMin + Margin, area.xMax - w - Margin));

            // 세로: 아래 우선, 공간 부족하면 위로 flip, 마지막에 경계 clamp(안전망)
            float below = a.yMax + Gap;
            float above = a.yMin - Gap - h;
            bool fitsBelow = below + h <= area.yMax - Margin;
            bool fitsAbove = above >= area.yMin + Margin;
            float top = fitsBelow ? below : (fitsAbove ? above : below);
            top = Mathf.Clamp(top, area.yMin + Margin, Mathf.Max(area.yMin + Margin, area.yMax - h - Margin));

            Vector2 local = _root.WorldToLocal(new Vector2(left, top));
            _layer.style.left = local.x;
            _layer.style.top = local.y;
            _layer.style.visibility = Visibility.Visible;
        }

        public void Dispose()
        {
            _root.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            _layer.RemoveFromHierarchy();
            CurrentAnchor = null;
        }
    }
}
