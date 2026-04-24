# 📝 인터페이스 정의서: ITurnManager (V1.0)

본 문서는 전투 시스템의 핵심 로직인 턴 및 라운드 관리를 담당하는 `ITurnManager` 인터페이스의 명세와 의존성 구조를 정의합니다.

---

### 1단계: 기능 인터페이스 및 의존성 설계
* **인터페이스 목적**: 유닛들의 행동 순서를 계산하고, 턴/라운드 흐름을 제어하여 공정한 전투 진행을 보장함.
* **주요 메서드 및 속성**:
    ```csharp
    public interface ITurnManager
    {
        // --- 데이터 관리 ---
        /// <summary>전투에 참여할 유닛을 등록합니다.</summary>
        void RegisterUnit(ITurnUseUnit unit);
        
        /// <summary>현재 턴 큐에 대기 중인 유닛 목록입니다.</summary>
        IReadOnlyList<ITurnUseUnit> TurnQueue { get; }

        // --- 흐름 제어 ---
        /// <summary>등록된 유닛들을 바탕으로 전투 루프를 시작합니다.</summary>
        UniTask StartBattleAsync(CancellationToken cancellation);
        
        /// <summary>전투를 강제로 중단합니다.</summary>
        void StopBattle();
		
		/// <summary>전투 종료.</summary>
        void EndBattle();

        // --- 이벤트 ---
        /// <summary>턴 큐가 갱신될 때마다 발생합니다. (UI 연동용)</summary>
        event Action OnTurnQueueUpdated;
    }
    ```

### 2단계: 의존성 맵 (Input/Output)

| 구분 | 항목 (시스템/파트) | 전달 데이터 및 신호 내용 (Input/Output) |
| :--- | :--- | :--- |
| **선행 (Input)** | `IBattleEntryPoint` | 전투 시작 시 참여 유닛 리스트(`IEnumerable<ITurnUseUnit>`) 전달 |
| **선행 (Input)** | `ITurnUseUnit` (BaseCharacter) | 유닛의 속도(Speed) 및 행동 가능 여부(IsAlive, CC상태 등) 데이터 |
| **후행 (Output)** | `TurnUI` | 턴 큐 갱신 신호(`OnTurnQueueUpdated`) 및 현재 큐 데이터 전달 |
| **후행 (Output)** | `ITurnUseUnit` (BaseCharacter) | 유닛의 행동 시작 신호 (`OnTurnStart`) 트리거 |

---

### 3단계: 독립 테스트 (Visual-less 검증 계획)
* **Mock 데이터 활용**: `ITurnUseUnit`의 속도 값을 다양하게 설정한 Mock 객체들을 등록하여 턴 순서 정렬 로직 검증.
* **로그 검증**: 
    - `RegisterUnit` 시 큐에 정상적으로 들어오는지 확인.
    - `StartBattleAsync` 실행 시 라운드 증가 및 턴 교체가 로그로 출력되는지 확인.
    - 유닛 사망 시 큐에서 즉시 제거되는지 확인.

---

### 💡 협업 가이드
- **기다리지 마세요**: UI 개발자는 `ITurnManager.OnTurnQueueUpdated` 이벤트만 구독하여 행동 순서 리스트를 미리 구현할 수 있습니다.
- **유연하게 대처하세요**: 턴 결정 방식(속도 기반, 틱 방식 등)이 변경되더라도 인터페이스 메서드는 유지하여 외부 시스템의 수정을 최소화합니다.
