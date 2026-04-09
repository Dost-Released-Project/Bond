using System.Collections.Generic;
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
    private TurnManager _turnManager;
    
    // 1. 수정됨: 단일 핸들이 아닌, 슬롯 개수만큼의 핸들 배열로 변경!
    private AsyncOperationHandle<Sprite>[] _imageHandles;

    private void Awake()
    {
        // 2. 핸들 배열을 초상화 슬롯 개수와 동일하게 초기화
        _imageHandles = new AsyncOperationHandle<Sprite>[portraitSlots.Length];
    }

    [Inject]
    public void Construct(TurnManager turnManager)
    {
        _turnManager = turnManager;
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
        // 내 슬롯(index)에 이미 불러오던 이미지가 있다면 메모리 해제
        if (_imageHandles[index].IsValid())
        {
            Addressables.Release(_imageHandles[index]);
        }

        // 내 슬롯 전용 핸들에 할당
        _imageHandles[index] = Addressables.LoadAssetAsync<Sprite>(address);

        await _imageHandles[index].ToUniTask();

        if (_imageHandles[index].Status == AsyncOperationStatus.Succeeded)
        {
            // 3. 새로운 이미지로 교체합니다.
            targetImage.sprite = _imageHandles[index].Result;
            
            // 4. 로드가 완벽히 끝났으니 Image 컴포넌트를 다시 켜서 화면에 보여줍니다!
            targetImage.enabled = true;
        }
        else
        {
            Debug.LogError($"[TurnUI] 이미지 로드 실패: {address}");
        }
    }

    private void OnDestroy()
    {
        if (_turnManager != null)
        {
            _turnManager.OnTurnQueueUpdated -= UpdateTurnUI;
        }

        // 5. 파괴될 때 모든 슬롯의 핸들을 순회하며 메모리 해제
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