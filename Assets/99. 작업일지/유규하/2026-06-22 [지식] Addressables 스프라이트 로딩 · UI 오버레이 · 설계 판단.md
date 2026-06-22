# 리액션 UI 작업에서 정리한 것 — 지식 문서

날짜: 2026-06-22. 특정 기능에 묶이지 않는 재사용 가능한 메커니즘·판단을 모았다. 구현 상세는 같은 날짜 [기술] 문서 참고.

---

## 1. Addressables 스프라이트 로딩

UI에서 `Addressables.LoadAssetAsync<Sprite>(address)`를 직접 부르는 코드가 여러 프레젠터에 흩어져 있다(portrait·roster·embark·전투패널·리액션). 그 과정에서 확인한 동작:

### 1.1 같은 키는 메모리에 중복되지 않는다 (dedup)
- 같은 `address`로 여러 번 `LoadAssetAsync`를 불러도 Addressables는 **에셋을 한 번만** 메모리에 올리고 **동일한 Sprite 인스턴스**를 돌려준다.
- 즉 "스킬 그리드"와 "리액션 행동 아이콘"이 같은 스킬 스프라이트를 써도 **메모리 중복 없음**. 두 번째 호출은 캐시 히트.
- → "각 UI가 같은 아이콘을 따로 로드하면 메모리 낭비"는 **사실이 아니다**.

### 1.2 핸들 ref count는 호출마다 쌓인다
- dedup은 에셋 메모리 얘기고, **핸들(ref count)은 호출마다 따로 증가**한다. `LoadAssetAsync` = +1, `Addressables.Release(handle)` = −1.
- 이 프로젝트의 UI 스프라이트 코드는 **핸들을 release하지 않는 관례**(앱 종료까지 유지)다. `LoadPortraitAsync` 등 전부 동일.
- 진짜 문제는 "해제 안 함"보다 **같은 주소를 반복 로드해 ref가 누적**되는 것. 두 가지로 대응:
  - **재로드 가드**: 고정 UI 요소(슬롯 아이콘처럼 재사용되는 `Image`)는 `이전주소 == 새주소`면 로드를 생략 → 주소당 ~1회 로드. (portrait가 쓰던 패턴.)
  - 통째로 재생성되는 UI(그리드 `Clear()`+재빌드, 드롭다운)는 재로드 가드를 못 건다(매번 새 요소). 여기서 ref가 더 빨리 쌓인다.

### 1.3 churn — release-후-즉시-재로드
- "Clear할 때 핸들을 release하면 누적이 사라지지 않나?" → 맞다. 다만 순진하게 하면 **churn**(버렸다가 곧바로 다시 만드는 낭비)이 생긴다.
- 통째 재빌드 UI에서 Clear→release→**ref 0→언로드**→재빌드→같은 주소 **다시 로드**(번들 재읽기)→비동기 로드 동안 **빈 칸 깜빡임**.
- 그 스프라이트를 **다른 UI도 잡고 있으면** ref가 0이 안 돼 언로드 안 됨 → churn 없음(안전). 즉 churn은 "그 화면 하나만 쓰는 아이콘"에서만.
- 깔끔한 해법 = **ref-count 공유 캐시**(`Acquire`/`Release`): 마지막 1개일 때만 언로드 → 공유 아이콘은 churn 없음. (아직 미구현 — 전 UI 공통 리팩터라 별도 작업.)

### 1.4 없는 키 = Addressables가 *스스로* 예외를 로그한다
- 없는 키로 `LoadAssetAsync`를 부르면, **caller의 try/catch와 무관하게** Addressables가 내부적으로 `InvalidKeyException` 스택트레이스를 콘솔에 찍는다(`No Location found for Key=...`).
- 그래서 `await ...ToUniTask()`를 try/catch로 잡아 경고를 따로 내면 **콘솔에 2줄**(Addressables 에러 + 내 경고)이 된다.
- **해결: 없는 키로 LoadAssetAsync를 아예 부르지 않기.** `Addressables.LoadResourceLocationsAsync(key)`는 **없는 키여도 예외·로그 없이 빈 목록**을 반환하는 표준 "존재 확인" API. 먼저 이걸로 확인하고 유효할 때만 로드하면 콘솔 메시지는 우리 경고 하나뿐.

```csharp
var locHandle = Addressables.LoadResourceLocationsAsync(address);
var locations = await locHandle.ToUniTask();
bool found = locations != null && locations.Count > 0;
Addressables.Release(locHandle);
if (!found) { /* 우리 경고 1줄 */ return null; }
return await Addressables.LoadAssetAsync<Sprite>(address).ToUniTask();
```

- 전역으로 `ResourceManager.ExceptionHandler`를 죽여 로그를 막는 방법도 있으나, **다른 시스템의 진짜 에러까지 숨겨** 금지.

### 1.5 `SpriteLoader`는 캐시가 아니다
- `ISpriteLoader`/`SpriteLoader`(`98. LeeJuno/Common`)가 있지만 **무상태 얇은 래퍼** — `LoadAssetAsync`를 try/catch로 감싸 **핸들을 caller에게 반환**할 뿐, **캐시·dedupe 없음**. `02. Scripts` UI는 아무도 안 쓴다.
- → 이걸 일부에만 끼워도 중복이 줄지 않고(이미 Addressables가 dedupe) 패턴만 반쪽 불일치. "공유 캐시"는 별도로 만들어야 한다.

### 1.6 의사결정 요약
- 같은 스프라이트 중복 로드? → **메모리 문제 아님**(dedup). 신경 X.
- 고정 요소 반복 로드? → **재로드 가드**로 충분.
- 없는 키 로그 스팸? → **`LoadResourceLocationsAsync` 존재확인**.
- 핸들 누적이 거슬리는 시점? → 그때가 **ref-count 공유 캐시** 도입 신호(전 UI 공통). 그 전엔 현재 관례 유지.

---

## 2. UI Toolkit — 떠있는 드롭다운 vs 인라인 풀

### 2.1 `TooltipPopup`을 클릭 드롭다운으로 못 쓴다
- `TooltipPopup`은 콘텐츠와 레이어의 `pickingMode`를 **`Ignore`로 강제**한다(툴팁이 입력을 가로채면 안 되니까). → **클릭 가능한 항목**을 담는 드롭다운엔 부적합.
- 대신 배치 수학(anchor 아래 우선·공간 없으면 위로 flip·패널 경계 clamp)은 그대로 베껴 **picking 살린 별도 오버레이**(`SlotDropdown`)를 만들었다.

### 2.2 인라인 풀 vs 오버레이
- 인라인 풀(슬롯 사이에 끼워 펼치는 요소)은 **레이아웃을 밀어** 다른 슬롯이 출렁인다. 목록이 길수록 거슬림.
- 오버레이(문서 루트 절대배치)는 **레이아웃 비파괴** + body의 `overflow:hidden`/ScrollView에 안 잘림. → 드롭다운엔 오버레이가 맞다.

### 2.3 바깥클릭 닫기 + 토글 (capture phase 타이밍)
- 닫기: 문서 루트에 `PointerDownEvent`를 **capture(TrickleDown)** 로 걸고, 타깃이 레이어/앵커 바깥이면 Close.
- 여는 클릭에 안 닫히는 이유: capture PointerDown은 **pointer down** 시점, 여는 동작은 항목의 **ClickEvent(=up)**. down 시점엔 아직 안 열려 있어 가드에서 빠진다.
- 토글(같은 앵커 재클릭): down(capture)에서 앵커는 예외 처리로 유지 → up의 ClickEvent에서 `CurrentAnchor == anchor`면 Close. 다른 곳 클릭 시엔 down에서 닫히고 그 요소의 ClickEvent가 새로 연다(전환 자연스러움).

### 2.4 문서 루트에 붙여야 USS 스코프가 산다
- 오버레이를 **anchor가 속한 문서 루트(UIDocument.rootVisualElement)** 에 붙여야 그 문서의 `<Style>` USS 스코프 안이라 클래스 스타일이 정상 적용된다. (TooltipPopup도 같은 이유로 문서 루트에 레이어를 심는다.)

---

## 3. 설계 판단 (왜 이렇게)

### 3.1 분할 문구: 파생 + 오버라이드
- 조건/행동 텍스트를 기존 `Trigger.Description`/`Effect.Description`에서 파생할 수 있지만 **디버그체**다(조건=`"X && Y"`, `"Conditions are empty"`, SkillCast=`"스킬 #N 발동"`).
- 그래서 "파생 + 정의별 오버라이드"로 가되, **현실적으로 오버라이드가 주력, 파생은 폴백.** 단일 출처 욕심보다 **표시 품질**을 택함.

### 3.2 미할당 = 빨강인 이유
- 편집칸이 비면 **리액션 자체가 발동하지 않는다.** 단순 "todo"가 아니라 **불발 경고**라서, 호박색(`--stress-warn`)보다 강한 빨강이 맞다.
- 테마엔 슬롯 도메인의 "미설정/경고" 텍스트 토큰이 없어 신규 `--slot-unset`(#c05030) 추가.

### 3.3 할당 시: 이름으로 바꾸지 않고 문구 유지 + 아이콘 추가
- 3분할은 항상 같은 서술 문장("지정 아군이 / 공격 받을때 / 대신 맞는다")으로 읽히고, **구체적 대상은 아이콘(+툴팁)으로 식별**.
- 그래서 칸 텍스트는 할당과 무관하게 고정. 할당이 바꾸는 건 **색 + 아이콘(빈→채움)** 둘뿐. 로직이 단순해지고 일관성↑.

---

## 4. 스타일 가이드: 변수 추가 절차 (재확인)

- 색은 **사용 위치 도메인**에 맞는 변수만. 없으면 **차용 금지, 신규 추가**(`--slot-*`, `--text-*` …).
- 신규 변수 = **매니페스트(`EoB_Variables.uss`)에 이름** + **현재 테마(`EoB_Theme_DarkAmber.uss`)에 값** 양쪽. 이름만/값만 추가 금지(이름만 빠지면 placeholder `transparent`로 사라짐).
- 값은 기존과 같아도 됨(의미 토큰이라 중복 허용). `--slot-unset`=#c05030는 `--stress-crit`과 같은 톤 재사용.
- 추가하면 **이름·값·이유·파일**을 작업 결과에 보고(가이드 MD와 매니페스트는 수동 동기화).