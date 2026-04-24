# 📝 인터페이스 정의서: IBattleManager (V1.0)

본 문서는 전투 중 발생하는 수치 계산 및 스킬 로직 처리를 담당하는 `IBattleManager` 인터페이스의 명세와 의존성 구조를 정의합니다.

---

### 1단계: 기능 인터페이스 및 의존성 설계
* **인터페이스 목적**: 전투 내의 모든 로직(데미지 계산, 스킬 효과 적용, 상태 이상 판정 등)을 중앙에서 처리하여 데이터의 정합성을 보장함.
* **주요 메서드**:
    ```csharp
    public interface IBattleManager
    {
        /// <summary>전투 상황(Context)에 따라 스킬 로직을 적용합니다.</summary>
        /// <param name="context">스킬 시전자, 대상, 스킬 데이터 등을 포함한 컨텍스트</param>
        void SkillApplyLogic(BattleContext context);

        /// <summary>유닛의 능력치와 상태를 기반으로 최종 데미지를 계산합니다.</summary>
        float CalculateDamage(ITurnUseUnit attacker, ITurnUseUnit target, float basePower);

        /// <summary>특정 대상에게 상태 이상이나 버프를 부여합니다.</summary>
        void ApplyEffect(ITurnUseUnit target, IEffectData effect);
    }
    ```

### 2단계: 의존성 맵 (Input/Output)

| 구분 | 항목 (시스템/파트) | 전달 데이터 및 신호 내용 (Input/Output) |
| :--- | :--- | :--- |
| **선행 (Input)** | `ITurnManager` | 현재 턴 유닛의 스킬 사용 의사 결정 및 타겟팅 신호 수신 |
| **선행 (Input)** | `ISkillManager` | 사용하려는 스킬의 기본 데이터(계수, 효과 정보 등) |
| **후행 (Output)** | `ITurnUseUnit` (BaseCharacter) | 계산된 데미지 적용(HP 감소) 및 상태 효과(버프/디버프) 리스트 업데이트 |
| **후행 (Output)** | `BattleUI` | 데미지 텍스트 출력 요청 및 유닛 체력 바 갱신 신호 |
| **후행 (Output)** | `EffectVisualizer` | 스킬 적중 시 VFX/SFX 재생 트리거 신호 |

---

### 3단계: 독립 테스트 (Visual-less 검증 계획)
* **Mock 데이터 활용**: 다양한 공격력/방어력을 가진 Mock 유닛들을 생성하여 데미지 공식이 의도대로 작동하는지 검증.
* **로그 검증**: 
    - `SkillApplyLogic` 호출 시 시전자와 대상의 데이터가 정상적으로 참조되는지 확인.
    - 크리티컬, 회피 등 확률 요소가 포함된 로직의 기대값 통계 확인.
    - 데미지 계산 후 유닛의 HP가 정확한 수치만큼 감소했는지 로그로 출력.

---

### 💡 협업 가이드
- **기다리지 마세요**: 전투 연출(VFX) 파트는 `IBattleManager`의 로직 결과(데미지 값, 상태 변화 여부)만 인터페이스로 받아 연출을 미리 구성할 수 있습니다.
- **유연하게 대처하세요**: 데미지 공식이나 스킬 효과 로직이 복잡해질 수 있으므로, `BattleContext`와 같은 데이터 컨테이너를 활용하여 인터페이스 확장을 최소화합니다.
