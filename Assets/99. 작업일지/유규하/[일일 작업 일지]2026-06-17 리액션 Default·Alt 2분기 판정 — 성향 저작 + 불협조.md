# 리액션: 판정으로 행동이 갈리는 구조로 (2026-06-17)

리액션이 "조건 충족 → 저장된 행동 즉시 실행"이던 걸, **행동을 둘(평상시 / 대체) 들고 있다가 실행 시점에 확률 판정으로 하나를 고르는** 구조로 바꿨다. 이 변화 하나에서 출발해 성향 3종(002·004·014)을 새 구조로 저작했고, 그러는 데 필요한 부품(무행동 효과 · 무작위 공격 · 특정 아군 불협조)을 만들었으며, 그 과정에서 책임 분리 한 건을 정리했다.

## 핵심 변화 — 단일 행동에서 판정 분기로

```
[전]  트리거 충족 → Reaction.Effect 실행
                    (성향은 돌발 굴림 실패 시 그냥 버려짐 = 아무것도 안 함)

[후]  트리거 충족
        └ JudgeReaction(reaction, 관찰대상)
             역할 → 확률로  Anomaly       | Default
             성향 → 확률로  BondAwakening | Default
        └ EffectFor(결과):  Default → BaseEffect / 그 외 → AltEffect (없으면 Base 폴백)
```

- `Reaction.Effect`(단일) → `BaseEffect`(평상시 = 플레이어가 편집하는 UserSkill) + `AltEffect`(대체 = 디자이너 저작). 기존 저작값은 `[FormerlySerializedAs("Effect")]` 로 BaseEffect에 보존.
- **트리거만 맞으면 항상 실행을 만든다** — 굴림 실패로 버리는 `continue` 제거. 그래서 성향도 평상시(Default)엔 BaseEffect를 실제로 수행한다(이전엔 굴림 실패 시 무동작).
- `ReactionResult`: `Success` → **`Default`**(평상시)로 개명, 그 외 `Anomaly`/`BondAwakening`.
- 역할/성향을 데이터·실행에서 쪼개지 않는다. 차이는 아래 판정 함수와 결과 태그 두 곳뿐.

## 판정 규칙 — 역할과 성향은 반대로 흔들린다

서사가 비대칭이라 확률 공식의 방향도 반대다 (`BaseCharacter.Anomaly.cs`):

- **역할** = 관계가 **낮을수록** 특이(돌발) 행동(`Anomaly`). `GetAnomalyChance` = base + 스트레스 − INT − 관계.
- **성향** = 관계가 **높을수록** 강화 행동(`BondAwakening`). `GetBondAwakeningChance` = base + 관계 + 스트레스 + INT.
- 관계 수치(`RelationFor`) = 리액터 ↔ 관찰 대상 평균, 대상 없으면 파티 평균.
- ⚠ 계수·`relation` 스케일 전부 미튜닝(구조 + 합리적 기본값 + `// TODO`).

자기 턴 경로(`TryRunSelfTurnAnomalyAsync`)도 같은 판정을 쓴다(Default = 계획 행동 유지 / 대체 = 오버라이드). OnSelfTurn 트레잇의 정확한 의미 정리(특히 TRT_001)는 사용자 영역으로 남김.

## 새 구조를 채우려고 만든 부품

성향을 실제로 저작하려니 기존 어휘로 안 되는 게 셋 있었다:

- **무행동 `NoActionReactionEffect`** — 대체 분기가 "연출만, 행동 없음"일 수 있어서. `null`(미저작)과 구분되는 명시적 무동작. 실행부가 결과로 효과를 고른 뒤 `null`만 스킵하므로, 무행동은 **연출은 재생되고 행동만 생략**된다.
- **무작위 공격 `RandomAttackSkillReactionEffect`** — 호전적의 "추가 공격"용. ① 현재 위치에서 사용 가능(`UseableSlots & rank`) ② 적 공격(`Target==Enemy`) ③ 그 타겟에 실제로 닿음(`CanSkillTarget`) — **셋 다 만족하는 장착 스킬 중 무작위 1개**. 없으면 무행동(= "제자리에서 공격 못 하면 안 함").
- **불협조 Distrust** — 의심많은의 "그 아군만 안 도움". 봉인과 동형의 런타임 상태(`BaseCharacter.Distrust.cs`: `ApplyDistrust`/`IsUncooperativeWith`/`TickDistrust`, 자기 턴에 만료). 효과는 `DistrustReactionEffect`.

## 저작한 성향 (001은 사용자가 직접)

| Trait | 조건 | 평상시 `.Do` | 대체 `.Alt` |
|---|---|---|---|
| 002 호전적 | 적 처치 | 전열 이동 + 무작위공격(전열 적) | 자기 공격버프(+30%/2턴) + 제자리 무작위공격 |
| 004 의심많은 | 아군 공격 빗나감 | 그 아군 불협조(1턴) | 무행동(대사 연출만) |
| 014 과시욕 | 본인 치명타 | 남은 리액션 봉인 + 스트레스 −10 | 봉인 없이 스트레스 −5 |

DSL(`ReactionDefBuilder`): `.Do`=Base, `.Alt` 신규, `CastRandomAttack`/`Distrust`/`NoAction` 팩토리. Alt 없는 리액션은 정상(평상시만, 굴림 시 Base 폴백) — 그래서 "Alt 미저작" 경고는 노이즈라 뺐다.

## 결정 — 불협조를 어디서 막을까

불협조는 둘을 막아야 한다: (1) 그 아군 행동에 대한 리액션, (2) 그 아군을 겨눈 보호/보조 스킬 선택(수동·자동 모두).

처음엔 (2)를 `FormationManager.GetValidSlots`/`HasAnyValidTarget`에 직접 넣었다 — 수동 하이라이트·자동 AI·스킬 가용성이 전부 거치는 단일 지점이라 한 방에 막혀서. 하지만 FormationManager는 진영·사거리 마스크만 보는 곳이고 관계 상태를 아는 건 책임 침범이다. 그래서 분리:

- `FormationManager` → **순수 기하**로 환원.
- 불협조 상태를 가진 **시전자**(`BaseCharacter`)에 `GetSelectableSlots`/`HasSelectableTarget`(= 기하 유효 ∩ 행동 허용)을 두고, **보호/보조(DEFENSIVE/SUPPORT)** 스킬일 때만 불협조 아군을 제외. 소비처(AI·가용성·수동 UI `BattleFormationPresenter`)를 이쪽으로 돌림.
- (1)은 `ReactionSystem.Resolve`에서 매치 후보 제외.
- 부수 효과: 적 공격 사거리 판정(`CanSkillTarget`)은 순수 `GetValidSlots`만 쓰게 되어 분리가 오히려 자연스러워짐.

## 이름 정리

- `SwingEffect` / `.Swing()` → **`AltEffect`** / **`.Alt()`** (대체).
- `ReactionResult.Success` → **`Default`**.

## 지금 상태 / 안 한 것

- 컴파일은 참조 대조로 확인. **Unity 재컴파일·런타임 동작은 미검증.** 트레잇 반영은 `Bond/Reactions/구현가능 성향 카탈로그 생성` 메뉴 재실행 필요.
- 남은 것: TRT_001 및 OnSelfTurn 자기턴 의미(사용자), 확률 계수 튜닝, `BondAwakening` 전용 연출, 이제 미사용이 된 `HasAnyValidTarget` 정리 여부.
- 신규 파일은 `BaseCharacter.Distrust.cs` 하나, 나머지는 기존 파일 수정.
