using System;
using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UnityEngine.UI;
using VContainer;

public class TurnUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI turnText;
    [SerializeField] private Image[] portraitSlots;

    // CONV-03: ITurnManager.TurnCount 프로퍼티가 인터페이스에 없으므로 구체 클래스 의존을 유지한다.
    // ITurnManager에 TurnCount를 추가한 뒤 private ITurnManager _turnManager; 로 교체하라.
    private TurnManager _turnManager;
    private ISpriteLoader _spriteLoader;

    // 슬롯 개수만큼의 핸들 배열
    private AsyncOperationHandle<Sprite>[] _imageHandles;
    // 슬롯별 CancellationTokenSource — 연속 로드 시 이전 작업 취소용
    private CancellationTokenSource[] _loadCancellations;

    private void Awake()
    {
        // 핸들 배열과 CancellationTokenSource 배열을 초상화 슬롯 개수와 동일하게 초기화
        _imageHandles = new AsyncOperationHandle<Sprite>[portraitSlots.Length];
        _loadCancellations = new CancellationTokenSource[portraitSlots.Length];
    }

    [Inject]
    public void Construct(TurnManager turnManager, ISpriteLoader spriteLoader)
    {
        _turnManager = turnManager;
        _spriteLoader = spriteLoader;
    }

    private void Start()
    {
        _turnManager.OnTurnQueueUpdated += UpdateTurnUI;
    }

    private void UpdateTurnUI()
    {
        IReadOnlyList<ITurnUseUnit> currentQueue = _turnManager.TurnQueue;
        
        turnText.SetText("{0}", _turnManager.TurnCount); 
        
        for (int i = 0; i < portraitSlots.Length; i++)
        {
            if (i < currentQueue.Count)
            {
                // 1. 슬롯 오브젝트는 켜서 자리를 차지하게 둡니다.
                portraitSlots[i].gameObject.SetActive(true);

                // 2. 핵심 로직: 이전 이미지가 보이지 않도록 Image 컴포넌트만 즉시 끕니다.
                portraitSlots[i].enabled = false;

                // 인덱스(i)를 같이 넘겨서 각 슬롯이 독립적인 핸들을 쓰게 함
                LoadPortraitAsync(i, portraitSlots[i], currentQueue[i].ImageAddress).Forget();
            }
            else
            {
                portraitSlots[i].gameObject.SetActive(false);
            }
        }
    }
    
    private async UniTaskVoid LoadPortraitAsync(int index, Image targetImage, string address)
    {
        // 이전 로드 작업을 취소한다 — 연속 호출 시 구 핸들 덮어쓰기 누수 방지
        _loadCancellations[index]?.Cancel();
        _loadCancellations[index]?.Dispose();
        _loadCancellations[index] = new CancellationTokenSource();
        CancellationToken token = _loadCancellations[index].Token;

        // 내 슬롯(index)에 이미 불러오던 이미지가 있다면 메모리 해제
        if (_imageHandles[index].IsValid())
            Addressables.Release(_imageHandles[index]);

        // ISpriteLoader 에게 로드를 위임. 핸들 소유권은 이 클래스가 유지한다.
        _imageHandles[index] = await _spriteLoader.LoadAsync(address);

        // 취소된 경우 핸들을 즉시 해제하고 종료
        if (token.IsCancellationRequested)
        {
            if (_imageHandles[index].IsValid())
                Addressables.Release(_imageHandles[index]);
            return;
        }

        if (_imageHandles[index].Status == AsyncOperationStatus.Succeeded)
        {
            targetImage.sprite = _imageHandles[index].Result;
            targetImage.enabled = true;
        }
        else
        {
            Debug.LogError($"[TurnUI] 이미지 로드 실패: {address}");
            if (_imageHandles[index].IsValid())
            {
                Addressables.Release(_imageHandles[index]);
                _imageHandles[index] = default;
            }
        }
    }

    private void OnDestroy()
    {
        if (_turnManager != null)
        {
            _turnManager.OnTurnQueueUpdated -= UpdateTurnUI;
        }

        // 진행 중인 모든 슬롯의 로드 작업 취소 및 CancellationTokenSource 해제
        if (_loadCancellations != null)
        {
            foreach (CancellationTokenSource cts in _loadCancellations)
            {
                cts?.Cancel();
                cts?.Dispose();
            }
        }

        // 파괴될 때 모든 슬롯의 핸들을 순회하며 메모리 해제
        if (_imageHandles != null)
        {
            for (int i = 0; i < _imageHandles.Length; i++)
            {
                if (_imageHandles[i].IsValid())
                {
                    Addressables.Release(_imageHandles[i]);
                }
            }
        }
    }
}