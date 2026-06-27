using System;
using System.Collections.Generic;
using System.Linq;
using VContainer;
using VContainer.Unity;
using UnityEngine;

namespace Bond.Tutorial
{
    public class CampTutorialEntryPoint : IInitializable
    {
        private readonly CampTutorialSystemController _tutorialController;

        [Inject]
        public CampTutorialEntryPoint(CampTutorialSystemController tutorialController)
        {
            _tutorialController = tutorialController;
        }

        public void Initialize()
        {
            // [수정] DBSORegistry 규칙 준수: 문자열이나 라벨 대신 
            // 이미 레지스트리에 로드된 전역 DB들을 가로질러 TutorialStepSO 타입만 쿼리해옵니다.
            // 마을 시퀀스(Sequence_A_Town) 데이터만 정밀 조준 쿼리
            var queryResult = DBSORegistry.QuerySO<TutorialStepSO>(so => so.Sequence == TutorialSequence.Sequence_C_Camp);
    
            List<TutorialStepSO> stepList = queryResult.ToList();

            if (stepList.Count > 0)
            {
                // ID 문자열 정렬 (TUT_A_01, TUT_A_02 순서 보장)
                stepList.Sort((x, y) => string.Compare(x.Id, y.Id, StringComparison.Ordinal));

                // 컨트롤러에 순사대로 배치 데이터 주입
                _tutorialController.SetupSteps(stepList);

                // 세이브 파일 동기화 후 최초 스텝(시퀀스 A-1) 트리거 가동
                _tutorialController.LoadProgress();
                
                Debug.Log($"<color=lime>[Tutorial EntryPoint]</color> DBSORegistry로부터 {stepList.Count}개의 TutorialStepSO 연동 성공.");
            }
            else
            {
                Debug.LogError("[Tutorial Error] DBSORegistry 내에 등록된 TutorialStepSO 데이터를 찾을 수 없습니다. DB 사전 로드 여부를 확인하세요.");
            }
        }
    }
}