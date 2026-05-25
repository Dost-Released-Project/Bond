# 통합 AI 작업 가이드라인 v1.0
> 원본: `제미나이_응답_가이드라인.md` · `제미나이_코드_작업_가이드.md` · `제미나이_문서_작성_가이드.md`  
> 작업 환경: Unity

---

## 1. 응답 워크플로우

**순서:** 요구사항 분석 → 해결 방안 제시 → 사용자 승인 → 수행 및 구현 → 교차 검증 → 최종 요약·가이드

| 단계 | 핵심 행동 |
|------|-----------|
| 요구사항 분석 | 의도를 정밀 분석. 모호한 지점은 추측 말고 질문으로 명확화 |
| 해결 방안 제시 | 최적 전략 + 논리적 근거(Rationale) 함께 제시 |
| 사용자 승인 | 확답 받은 후 실행 착수 |
| 교차 검증 | 완료 후 최초 요구사항과 수행 사항 1:1 대조. 누락 시 해결 방안 제시로 복귀 |
| 사후 가이드 | 결과물 역할 요약 + 바로 활용 가능한 실행 가이드 + Unity 하이어라키 계층 구조 포함 |

---

## 2. DLV 아키텍처 (Data-Logic-Visual) v0.2

**철학:** "입력은 흐르고, 데이터는 보존되며(Pure), 로직은 판단하고, 비주얼은 표현한다."

> 코드 작성 후엔 **테스트 케이스 가이드** 필수.  
> 네트워크 관련: 기능 작업 후 붙이는 방식. 기존 파일 있을 경우 `파일명 + 리팩토링 하겠습니다` 출력.

### 2-1. [D] 데이터 그룹

| 모듈 | 네이밍 | 형태 | 핵심 규칙 |
|------|--------|------|-----------|
| Pure Data | `[PureData]명` | ScriptableObject | Immutable(readonly/private set), Logic-Free |
| Pure DataBase | `[PureDataBase]명` | SO | `Dictionary<ID, PureData>` 보관, PureData 훼손 금지 |
| Runtime Data | `[RuntimeData]명` | — | PureData 읽기 전용 참조, Observable(C# event), Self-Update(Tick), ISaveableData 상속(필요 시) |
| Data Save-Load | — | — | ISaveableData 인터페이스, 저장·불러오기로만 RuntimeData 변경 |

### 2-2. [L] 로직 그룹

| 모듈 | 네이밍 | 핵심 규칙 |
|------|--------|-----------|
| Logic | `[System]` (예: ActionLogicSystem) | 판단만 수행, 값 직접 수정 금지. Interface 통해 Visual 호출. `Rigidbody`·`NavMeshAgent`·`transform.position` 직접 참조·수정 금지 |

### 2-3. [V] 비주얼 그룹

| 모듈 | 네이밍 | 핵심 규칙 |
|------|--------|-----------|
| Visual Interface | `I[Feature]Visualizer` | 추상 함수만. 구체 구현(2D/3D·NavMesh 등) 노출 금지. DIP 고리 역할 |
| World Visualizer | `[Feature]Visualizer` | 수동적(Passive). Interface 구현. 물리·이동의 실질 수행자 |
| UI View Layer | `[Feature]UIView` | Model event 직접 구독(Observer). 계산 로직 포함 금지 |

### 2-4. 데이터 흐름 파이프라인

```
Input(Pure Data 생성) → Process(Logic → Model 갱신) → Notify(event 발송)
→ Update UI(UIView → GUI 갱신) → Command(IVisualizer 호출) → Render(출력)
```

### 2-5. 개발 3대 원칙

- **Rule 1 – Everything is Data:** 모든 입력은 Pure Data 객체(struct/class)로 변환. 인자 나열 금지.
- **Rule 2 – Blind Logic:** LogicBehaviour는 Interface·event 통해서만 소통. 직접 참조 금지.
- **Rule 3 – Immutable Data:** Pure Data는 런타임에 절대 수정 금지. 변화는 Runtime Model 내 별도 변수 활용.

### 2-6. 바인딩 전략

| 종류 | 상황 | 전략 | 책임 |
|------|------|------|------|
| Internal Binding | Logic ↔ WorldVisualizer가 동일 프리팹 내 | Auto-Wiring (`Awake`에서 `GetComponent`) + `[RequireComponent]` | LogicSystem의 `Awake()` |
| External Binding | Logic(월드) ↔ UIView(캔버스) 서로 다른 계층 | Builder Pattern – 제3 클래스가 `UI.Bind(Model)` 호출 | `[Feature]Binder` |

---

## 3. 문서 작성 가이드 v0.1

**철학:** "기록은 흐름(Timeline)을 따르고, 요약은 논리(Why)를 따른다."

### 3-1. 작업 일지

**일일 작업 일지**
- 네이밍: `[일일 작업 일지]YYYY-MM-DD`
- 형태: Markdown
- 포함 항목: 작업 개요 / 문제·변경·성과(Before-After + **Why**) / 상세 사용 가이드 / 문제 재현 방법
- 규칙: **한 번 작성한 일지는 수정하지 않는다.** 동일 작업이 A→A`→A``로 3회 진행됐다면 3회 모두 기록하고 최종 A``에 대한 가이드를 남긴다.
- 작성 시점 : 작업자 요청 시

**주간 작업 일지**
- 네이밍: `[주간 작업 일지]YYYY-MM-DD`
- 형태: Markdown
- 포함 항목: 작업 개요 / 문제·변경·성과(Before-After + 일일 일지 링크)
- 규칙: **Mermaid 문법** 기반 도표 작성 필수

### 3-2. 기술 문서

**특정 시스템 기술 문서**
- 네이밍: `[기술 문서][Feature]`
- 역할: 주간·일일 일지 및 스크립트 확인 후 작성
- 해결책 제시 시 **왜 이 기술인가** 타당성 명시 (단순 구현은 생략 가능)
- **Mermaid 문법** 도표 활용

**전체 시스템 통합 기술 문서**
- 네이밍: `[프로젝트 통합 기술 문서]`
- 역할: 모듈·피처 단위 큰 그림 + 데이터 흐름 중심
- **Mermaid 문법** 도표 활용

---

## 4. 코드 리뷰 양식

> 이 섹션은 가이드라인 준수 여부 확인용 테스트 양식(`가이드라인_테스트.md`)을 포함합니다.

### 리뷰 기본 정보

| 항목 | 내용 |
|------|------|
| 리뷰 일시 | YYYY-MM-DD HH:MM (KST) |
| PR / 커밋 번호 | #000 / commit-hash |
| 작성자 | @username |
| 리뷰어 | @reviewer |
| 대상 파일/브랜치 | `src/example` (feature/branch-name) |

### 가이드라인 참고 여부

| 항목 | 내용 |
|------|------|
| 참고 여부 | ✅ 참고함 / ❌ 미참고 |
| 참고 시각 | YYYY-MM-DD HH:MM (KST) |

**참고한 가이드라인 파일 목록 (파일명 기준):**
- `참고 파일 명1.md`
- `참고 파일 명2.md`

> 파일 내부 제목이 아닌 **파일명**으로 기재합니다.

### 원본 유저 프롬프트

> 요약·생략 없이 프롬프트 전문을 그대로 기록합니다.

```
여기에 유저가 입력한 프롬프트를 한 글자도 빠짐없이 그대로 붙여넣기합니다.
줄바꿈, 띄어쓰기, 특수문자 포함 원문 그대로 유지.
```

### 리뷰 체크리스트

- [ ] **네이밍 컨벤션 준수** — DLV 네이밍 규칙(Prefix/Suffix) 적용 여부
- [ ] **에러 핸들링 일관성** — 예외 처리 방식이 프로젝트 정책과 일치하는가
- [ ] **테스트 커버리지 충족** — 코드 작성 후 테스트 케이스 가이드 포함 여부
- [ ] **아키텍처 원칙 준수** — Rule 1·2·3 및 바인딩 전략 준수 여부
- [ ] **코드 복잡도 적정 수준** — 함수 길이, 중첩 깊이, 단일 책임 원칙

### 작성자에게 전달할 개선 지시 프롬프트

> 리뷰어가 코드를 직접 작성하지 않습니다.  
> **무엇을, 왜, 어떤 기준에 따라** 수정해야 하는지를 프롬프트 형태로 전달합니다.

**개선 지시 #1 — [문제 영역 요약]**
```
[파일명 또는 함수명]의 [구체적 문제]를 수정해주세요.

현재: [문제가 되는 현상 또는 패턴 설명]
기대: [어떤 방식으로 구현되어야 하는지 설명]
참고: [관련 가이드라인 섹션 번호 또는 규칙명]
```

### 종합 의견 및 승인 여부

| 항목 | 내용 |
|------|------|
| 판정 | ✅ Approved / 🔄 Changes Requested / ❌ Rejected |
| 우선순위 | 🔴 Critical / 🟡 Major / 🟢 Minor / 💬 Nit |
| 종합 코멘트 | (자유 형식) |
