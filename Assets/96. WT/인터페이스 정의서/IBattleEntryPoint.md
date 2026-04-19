# 📝 인터페이스 정의서: IBattleEntryPoint (V1.0)

본 문서는 전투 시스템의 진입점인 `IBattleEntryPoint` 인터페이스의 명세와 의존성 구조를 정의합니다.

---

### 1단계: 기능 인터페이스 및 의존성 설계
* **인터페이스 목적**: 외부 시스템으로부터 전투 참여 데이터를 받아 전투 시스템의 전체 라이프사이클을 기동함.
* **주요 메서드**:
    ```csharp
    public interface IBattleEntryPoint
    {
        public UniTask StartAsync(CancellationToken cancellation, IEnumerable<ITurnUseUnit> unit);
    }
    ```

### 2단계: 의존성 맵 (Input/Output)

| 구분 | 항목 (시스템/파트) | 전달 데이터 및 신호 내용 (Input/Output) |
| :--- | :--- | :--- |
| **선행 (Input)** | `BattleFlowManager` | 전투 시작 명령 및 참여 유닛 리스트 (`IEnumerable<ITurnUseUnit>`) |
| **후행 (Output)** | `TurnManager` | **(핵심)** 수신한 유닛들을 등록(`RegisterUnit`)하고 전투 루프(`StartBattleAsync`)를 기동함 |

---
