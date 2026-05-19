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
            // H 키를 누르면 이벤트 발생 시뮬레이션
            if (Keyboard.current.hKey.wasPressedThisFrame)
            {
                Debug.Log("[JournalTestRunner] 'H' 키 입력 감지: 탐색 이벤트 트리거");

                // 1. 이벤트 데이터 세팅 (여러 건의 탐색 이벤트를 발생시킴)
                _locationProvider.SetDiscovery("EVT_FIND_ITEM", "어두운 숲", "낡은 지도");
                _locationProvider.SetDiscovery("EVT_FIND_ITEM", "버려진 야영지", "부러진 화살");

                // 2. 일지 수집 및 송출
                _journalSystem.CollectDailyLogs();
            }
        }
    }
}