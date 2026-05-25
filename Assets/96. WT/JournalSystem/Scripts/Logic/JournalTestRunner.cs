using System;
using UnityEngine;
using UnityEngine.InputSystem;
using VContainer;
using VContainer.Unity;

namespace Bond.WT.Journal
{
    /// <summary>
    /// [Test Logic] 런타임에 키보드 입력을 통해 일지 시스템의 전체 파이프라인을 테스트하는 진입점
    ///
    /// H 키: LocationEventProvider 경유 데이터 세팅 + 수집 (SetDiscovery → CollectDailyLogs)
    /// J 키: 현재 Provider 버퍼 기반 즉시 수집 — 결과창 즉시 호출 테스트
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

                // 1. 이벤트 데이터 세팅 (여러 건의 탐색 이벤트를 발생시키고, 메타데이터인 ItemId도 함께 전달)
                _locationProvider.SetDiscovery("EVT_FIND_ITEM", "어두운 숲", "낡은 지도", "08000000", 2);
                _locationProvider.SetDiscovery("EVT_FIND_ITEM", "버려진 야영지", "부러진 화살", "08000000");

                // 2. 일지 수집 및 송출
                _journalSystem.CollectDailyLogs();
            }

            // J 키를 누르면 현재 Provider 버퍼에 쌓인 데이터로 결과창을 즉시 호출한다
            // 주의: Provider 버퍼가 비어 있으면 결과창이 열리지 않는다 (CollectDailyLogs 내부에서 count == 0 조기 반환)
            // 사용 순서 예시: H 키로 데이터를 쌓은 후 J 키를 눌러 결과창 확인
            if (Keyboard.current.jKey.wasPressedThisFrame)
            {
                Debug.Log("[JournalTestRunner] 'J' 키 입력 감지: 현재 Provider 버퍼 기반 즉시 결과창 호출");
                _journalSystem.CollectDailyLogs();
            }
        }
    }
}