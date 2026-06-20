# TooltipPopup — 범용 툴팁 엔진

UI Toolkit 어떤 요소든 hover하면 툴팁을 띄워주는 **정적 엔진**. "어디에 / 어떻게 띄울지"(부착·실측·flip·경계 clamp·표시·숨김)만 담당하고, **툴팁 생김새는 호출부가 만든 `VisualElement`를 그대로 표시**한다. 즉 엔진은 스타일에 관여하지 않는다.

> 설계 이유·UI Toolkit 배경지식은 [설계노트.md](설계노트.md) 참고. 여기는 쓰는 법만.

---

## 빠른 시작

```csharp
// 요소에 hover하면 텍스트 툴팁 표시. 반환된 IDisposable로 해제.
IDisposable handle = TooltipPopup.Attach(chip, () => "롱소드\nSTR +3");

// 정리(OnDestroy / Dispose 등)
handle.Dispose();
```

`EquipSlotsPresenter`가 실제 사용 예다(슬롯마다 `Attach`, 핸들을 모아 `Dispose`).

---

## API

| 멤버 | 설명 |
|---|---|
| `Attach(target, Func<VisualElement> provider, prefer=Auto)` → `IDisposable` | **권장.** target hover 시 `provider()`가 만든 콘텐츠를 자동 표시/숨김 |
| `Attach(target, Func<string> textProvider, prefer=Auto)` → `IDisposable` | 위의 간단 텍스트 버전(기본 스킨) |
| `AttachFollow(target, provider/textProvider, prefer=Auto)` → `IDisposable` | `Attach`의 **마우스 추종** 버전(커서를 따라다님) |
| `Show(VisualElement content, VisualElement anchor, prefer=Auto)` | anchor 요소 기준으로 1회 표시 |
| `Show(string text, VisualElement anchor, prefer=Auto)` | 기본 스킨 텍스트로 1회 표시 |
| `ShowAt(VisualElement content, Vector2 panelPos, VisualElement context, prefer=Auto)` | 마우스 등 임의 좌표 기준 표시. `context`는 대상 문서 내 아무 요소(문서 판별용) |
| `Hide(IPanel panel)` / `Hide(VisualElement context)` | 숨김 |
| `BuildText(string)` → `Label` | 기본 `.tooltip` 스킨이 적용된 라벨 생성 |
| `enum Placement { Auto, Below, Above }` | 세로 배치 선호. Auto=아래 우선, 공간 없으면 위로 flip |

---

## 사용 패턴

**1. hover 자동 (대부분 이걸로)**
```csharp
_handles.Add(TooltipPopup.Attach(slot, () => GetTooltipText(slot)));
```

**2. 리치 콘텐츠 (생김새를 직접 구성)**
```csharp
TooltipPopup.Attach(card, () =>
{
    var root = new VisualElement();
    root.Add(new Label("스킬명"));
    // ... 자유롭게 레이아웃/스타일 ...
    return root;
});
```

**3. 마우스 추종 (맵 위 자유 오브젝트 등)**
```csharp
TooltipPopup.AttachFollow(building, () => BuildingTooltipContent.Build(b));
```
(저수준이 필요하면 `ShowAt(content, 좌표, context)`를 직접 호출.)

**해제**: `Attach`가 돌려주는 `IDisposable`을 모아두었다가 `Dispose()`에서 한꺼번에 해제한다.

---

## 스타일 (기본 + 덮어쓰기)

엔진은 콘텐츠를 **anchor의 문서 루트**에 붙인다 → 그 문서의 USS가 그대로 적용된다(중요: [설계노트 §2.3](설계노트.md) 참고).

- **기본 스킨** `.tooltip` — `Resources/Bond_Tooltip.uss`(테마 변수). 문자열/`BuildText` 경로가 자동 적용.
- **덮어쓰기 3단계** (약→강):

  | 방식 | 코드 | 우선순위 | 테마변수 |
  |---|---|---|---|
  | ⓐ 인라인 | `l.style.borderColor = ...` | 항상 최우선 | ✗ |
  | ⓑ 추가 클래스 | `l.AddToClassList("tooltip--danger")` (문서 USS에 정의) | 캐스케이드/특이도 | ✓ |
  | ⓒ 완전 커스텀 | 자기 `VisualElement` 통째로 | — | 자유 |

  ```csharp
  TooltipPopup.Attach(chip, () =>
  {
      var l = TooltipPopup.BuildText("위험!");
      l.AddToClassList("tooltip--danger"); // var(--border-danger) 등
      return l;
  });
  ```

- **기존 클래스 그대로 쓰기**: 콘텐츠에 이미 있는 문서 클래스를 붙이면 그대로 먹는다(문서 루트 마운트라서).
  ```csharp
  () => { var l = new Label(text); l.AddToClassList("equip-slots__tooltip"); return l; }
  ```

---

## 동작 특성

- **실측 후 배치**: 콘텐츠 실제 크기를 `GeometryChangedEvent`로 측정한 뒤 위치 계산. 고정 추정이 없어 내용이 길어도 안 잘림.
- **flip**: 아래 공간 부족 → 위로. 그래도 넘으면 경계 clamp(최후 안전망).
- **clamp 영역**: 문서 루트(보통 전체화면).
- **문서별 레이어 1개** 재사용, `pickingMode=Ignore`(입력 안 가로챔). 동시에 보이는 툴팁은 하나.

---

## 제약 / 주의

- 마운트가 **anchor의 문서 루트** → 그 문서 안에서 최상단(`BringToFront`). `sortingOrder`가 더 높은 **다른 문서**가 있으면 그건 못 덮는다(현재 프로젝트엔 해당 없음 — 이유는 설계노트 §3-C).
- `ShowAt`은 anchor가 없으니 `context`(대상 문서 내 아무 요소)로 문서를 판별한다.
- 기본 스킨은 `Resources.Load<StyleSheet>("Bond_Tooltip")` 의존. **콘텐츠를 직접 넘기는 경로(②③)는 Resources 불필요.**

---

## 통합 현황

- ✅ 장비 (`EquipSlotsPresenter`) — 슬롯 앵커.
- ✅ 스킬 — 전투 패널(`CharacterCombatPanelPresenter`, 슬롯 앵커) + CharacterDetail 스킬 칩(마우스 추종, `AttachFollow`). 공유 빌더 `SkillTooltipContent.Build()`. 기존 `SkillTooltipView`(MonoBehaviour+자체 UIDocument)는 **삭제**.
- ⏸ 건물 (`BuildingTooltipView`, 97.Moon) — 미통합. 마우스 추종이라 `AttachFollow` 후보.
- ⏹ 트레잇 — 현재 **작동하는 툴팁 없음**. `tag.tooltip`(Unity 내장)을 set하지만 런타임(Unity 6.3 UIDocument)에선 렌더 안 됨 → "통합"이 아니라 `Attach`로 **신규** 추가 대상.
