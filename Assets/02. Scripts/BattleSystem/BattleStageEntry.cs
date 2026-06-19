using BattleSystem.Interface;
using Bond.Expedition;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer.Unity;

namespace BattleSystem
{
    public class BattleStageEntry : IBattleStageEntry, IPostStartable, ITickable
    {
        private readonly IBattleFlowManager m_battleFlowManager;
        private readonly ExpeditionPayload m_battlePayload;
        private readonly IFormationManager m_formationManager;
        private readonly IStageMonsterContext m_stageMonsterContext;
        private readonly MonsterFactory m_monsterFactory;
        private readonly ISkillEffectPool m_skillEffectPool;

        public BattleStageEntry(IBattleFlowManager expeditionFlowManager, ExpeditionPayload expeditionPayload
            , IFormationManager formationManager, IStageMonsterContext stageMonsterContext
            , MonsterFactory monsterFactory, ISkillEffectPool skillEffectPool)
        {
            m_battleFlowManager   = expeditionFlowManager;
            m_battlePayload       = expeditionPayload;
            m_formationManager    = formationManager;
            m_stageMonsterContext = stageMonsterContext;
            m_monsterFactory      = monsterFactory;
            m_skillEffectPool     = skillEffectPool;
        }

        void IPostStartable.PostStart()
        {
            CharacterSetting();
        }

        public void Tick()
        {
            if (Keyboard.current != null && Keyboard.current.digit1Key.wasPressedThisFrame)
            {
                Debug.Log("1번 키 눌림");
                BattleSwitch();
            }

            if (Keyboard.current != null && Keyboard.current.digit2Key.wasPressedThisFrame)
            {
                Debug.Log("2번 키 눌림");
                CharacterSetting();
            }
        }

        private void CharacterSetting()
        {
            // ── 플레이어 파티 (기존 로직 유지) ──────────────────────────────
            int playerCnt = m_battlePayload.Party.Count;
            BaseCharacter[] player = new BaseCharacter[playerCnt];
            for (int i = 0; i < playerCnt; i++)
            {
                player[i] = m_battlePayload.Party[i];
                Debug.Log($"{i}번째 파티원 {m_battlePayload.Party[i]}");
                m_formationManager.SetCharacterToSlot(player[i], E_BattleSide.Player, i);
            }

            // ── 적 파티 (IStageMonsterContext 경유 로드) ─────────────────────

            // 테스트 로그: 컨텍스트에서 읽어온 원본 ID 목록 확인
            Debug.Log(
                $"[BattleStageEntry] IStageMonsterContext 읽기\n" +
                $"  GroupId    : {m_stageMonsterContext.MonsterGroupId}\n" +
                $"  MonsterIds : [{string.Join(", ", m_stageMonsterContext.MonsterIds)}]"
            );

            BaseCharacter[] enemy = m_monsterFactory.Build(m_stageMonsterContext.MonsterIds);
            m_skillEffectPool.AddCharactersAsync(enemy).Forget();

            // 테스트 로그: Build() 결과 요약 (BaseCharacter.ToString() 미사용 — Profession null 주의)
            Debug.Log($"[BattleStageEntry] MonsterFactory.Build() 완료 — 생성된 몬스터 수: {enemy.Length}");

            for (int i = 0; i < enemy.Length; i++)
            {
                // 테스트 로그: 슬롯 배치 직전 각 몬스터 필드 확인
                Debug.Log(
                    $"[BattleStageEntry] 적 슬롯 배치\n" +
                    $"  슬롯 인덱스  : {i}\n" +
                    $"  ID           : {enemy[i].Id}\n" +
                    $"  Name         : {enemy[i].Name}\n" +
                    $"  Level        : {enemy[i].Level}\n" +
                    $"  RoleType     : {enemy[i].RoleType}\n" +
                    $"  STR={enemy[i].Stat.STR}  AGI={enemy[i].Stat.AGI}  INT={enemy[i].Stat.INT}"
                );
                m_formationManager.SetCharacterToSlot(enemy[i], E_BattleSide.Enemy, i);
            }

            // 컨텍스트 클리어 — 다음 스테이지 진입 시 이전 데이터 잔류 방지
            m_stageMonsterContext.Clear();

            // 테스트 로그: Clear() 후 컨텍스트가 비워졌는지 확인
            Debug.Log(
                $"[BattleStageEntry] IStageMonsterContext.Clear() 완료\n" +
                $"  GroupId 잔류 여부 : {(string.IsNullOrEmpty(m_stageMonsterContext.MonsterGroupId) == false ? "잔류 (비정상)" : "비어 있음 (정상)")}\n" +
                $"  MonsterIds 잔류 수 : {m_stageMonsterContext.MonsterIds.Count}"
            );

            CharacterRegister(player, enemy);
        }

        private void CharacterRegister(BaseCharacter[] playerCharacter, BaseCharacter[] enemyCharacter)
        {
            // ExpeditionFlowManager에 CharacterSetting에서 결합한 객체를 등록
            m_battleFlowManager.PartySetting(playerCharacter);
            m_battleFlowManager.EnemySetting(enemyCharacter);
            BattleSwitch();
        }

        private void BattleSwitch()
        {
            m_battleFlowManager.BattleSwitch();
        }
    }
}