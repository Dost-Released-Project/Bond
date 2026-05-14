using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Test Logic] 런타임에 키보드 입력을 통해 일지 시스템의 전체 파이프라인을 테스트하는 진입점
    /// </summary>
    public class JournalTestRunner : ITickable
    {
        private readonly LocationEventProvider _locationProvider;
        private readonly JournalSystem _journalSystem;

        [Inject]
        public JournalTestRunner(LocationEventProvider locationProvider, JournalSystem journalSystem)
        {
            _locationProvider = locationProvider;
            _journalSystem = journalSystem;
        }

        public void Tick()
        {
            // T 키를 누르면 이벤트 발생 시뮬레이션
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                Debug.Log("[JournalTestRunner] 'T' 키 입력 감지: 탐색 이벤트 트리거");

                // 1. 이벤트 데이터 세팅 (시트의 첫 번째 ID가 JE_001 이라고 가정)
                // 만약 시트에 등록한 ID가 다르다면 아래 "JE_001"을 해당 ID로 변경해야 합니다.
                _locationProvider.SetDiscovery("14000000", "어두운 숲", "낡은 지도");

                // 2. 일지 수집 및 송출
                _journalSystem.CollectDailyLogs();
            }
        }
    }
}
