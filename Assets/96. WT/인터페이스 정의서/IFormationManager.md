# 📝 인터페이스 정의서: IFormationManager (V1.0)

본 문서는 전투 진영(Formation) 내 유닛의 위치 관리 및 위치 기반 스킬 사용 판정을 담당하는 `IFormationManager` 인터페이스의 명세를 정의합니다.

---

### 1단계: 기능 인터페이스 및 의존성 설계
* **인터페이스 목적**: 유닛의 진영 위치 정보를 유지하고, 위치 기반의 게임 규칙(스킬 사거리, 진영 이동 등)을 처리함.
* **주요 메서드**:
    ```csharp
    public interface IFormationManager
    {
        // --- 위치 정보 조회 ---
        /// <summary>캐릭터의 현재 Rank(위치) 반환</summary>
        FormationMask GetCharacterRank(BaseCharacter character);
        
        /// <summary>특정 진영/위치의 캐릭터 조회</summary>
        BaseCharacter GetCharacterAt(e_BattleSide side, FormationMask rank);

        // --- 진영 변경 로직 ---
        /// <summary>두 캐릭터의 위치 교체</summary>
        void SwapFormation(BaseCharacter fromCharacter, BaseCharacter toCharacter);
        
        /// <summary>특정 캐릭터를 지정 위치로 이동</summary>
        void MoveCharacter(BaseCharacter character, e_BattleSide side, int targetIndex);
        
        /// <summary>빈 공간을 메우기 위해 캐릭터들을 전진 배치</summary>
        void ConsolidationFormation(e_BattleSide side);

        // --- 스킬/타겟 판정 ---
        /// <summary>캐릭터가 현재 위치에서 스킬 사용 가능한지 확인</summary>
        bool IsSkillUsable(BaseCharacter character, FormationMask skillUsableMask);
        
        /// <summary>대상이 스킬의 타겟 범위 내에 있는지 확인</summary>
        bool IsTargetable(BaseCharacter target, FormationMask targetMask);
    }
    ```

### 2단계: 의존성 맵 (Input/Output)

| 구분 | 항목 (시스템/파트) | 전달 데이터 및 신호 내용 (Input/Output) |
| :--- | :--- | :--- |
| **선행 (Input)** | `IBattleEntryPoint` | 전투 시작 시 초기 배치 정보 전달 |
| **선행 (Input)** | `BaseCharacter` | 위치 계산 및 판정의 대상이 되는 캐릭터 객체 데이터 |
| **선행 (Input)** | `SkillData` | 스킬의 사용 가능 위치 및 타겟 범위 마스크 데이터 |
| **후행 (Output)** | `IBattleManager` | 스킬 사용 가능 여부 판정 결과(`bool`) 전달 및 데미지 계산 시 위치 보너스 참조 |
| **후행 (Output)** | `IFormationVisualizer` | `Swap`, `Move`, `Consolidation` 발생 시 유닛의 연출적 위치 이동 신호 |

---

### 3단계: 독립 테스트 (Visual-less 검증 계획)
* **Mock 데이터 활용**: 실제 유닛 모델 없이 `BaseCharacter` 참조값과 인덱스만으로 위치 교체 및 이동 로직이 데이터 상에서 올바르게 반영되는지 검증.
* **로그 검증**: 
    - `GetCharacterRank` 호출 시 유닛의 저장된 위치와 반환 값이 일치하는지 확인.
    - `ConsolidationFormation` 실행 후 리스트 내의 빈 슬롯이 의도대로 제거되고 유닛들이 전진했는지 인덱스 로그 출력.
    - 비트마스크 연산을 통한 `IsSkillUsable` 판정이 모든 케이스(1~4번 열)에서 정확한지 확인.

---

### 💡 협업 가이드
- **기다리지 마세요**: 연출(Visualizer) 파트는 `Swap`이나 `Move` 메서드 호출 신호만 받아 실제 게임 화면에서의 부드러운 이동 연출을 미리 개발할 수 있습니다.
- **있는 그대로 사용하세요**: 본 인터페이스는 현재 구현된 기능을 기반으로 명세화되었으므로, 추가 기능이 필요한 경우 4단계(변경 공지) 절차를 따릅니다.
