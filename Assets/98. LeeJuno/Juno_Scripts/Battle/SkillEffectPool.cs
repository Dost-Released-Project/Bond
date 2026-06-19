using System.Collections.Generic;
using System.Threading;
using Cysharp.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public class SkillEffectPool : ISkillEffectPool
{
    private const int POOL_SIZE = 4;

    private readonly Dictionary<string, Queue<GameObject>> _idle
        = new Dictionary<string, Queue<GameObject>>();
    private readonly Dictionary<string, List<GameObject>> _active
        = new Dictionary<string, List<GameObject>>();
    private readonly HashSet<string> _warmUpAddresses = new HashSet<string>();
    private readonly List<AsyncOperationHandle<GameObject>> _handles
        = new List<AsyncOperationHandle<GameObject>>();

    public async UniTask WarmUpAsync(IReadOnlyList<BaseCharacter> party, CancellationToken cancellationToken = default)
    {
        HashSet<string> newAddresses = CollectAddresses(party);

        if (AddressSetEquals(newAddresses, _warmUpAddresses))
        {
            Debug.Log("[SkillEffectPool] 장착 스킬 변경 없음 — 기존 풀 재사용");
            return;
        }

        Clear();
        _warmUpAddresses.Clear();

        foreach (string address in newAddresses)
        {
            if (string.IsNullOrEmpty(address))
                continue;

            Queue<GameObject> queue = new Queue<GameObject>(POOL_SIZE);
            List<GameObject> active = new List<GameObject>(POOL_SIZE);

            for (int i = 0; i < POOL_SIZE; i++)
            {
                AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address);

                try
                {
                    GameObject instance = await handle.ToUniTask(cancellationToken: cancellationToken);
                    instance.SetActive(false);
                    instance.transform.localScale = Vector3.one * 10f;
                    // Canvas 위에 렌더링되도록 SpriteRenderer 소팅 오더를 높게 설정한다
                    foreach (SpriteRenderer sr in instance.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.sortingOrder = 1;
                    // 전투씬 언로드 시 파괴되지 않도록 DontDestroyOnLoad 씬에 보관한다
                    Object.DontDestroyOnLoad(instance);
                    queue.Enqueue(instance);
                    _handles.Add(handle);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SkillEffectPool] 프리팹 생성 실패 address={address}: {e.Message}");
                }
            }

            _idle[address] = queue;
            _active[address] = active;
            _warmUpAddresses.Add(address);
        }

        Debug.Log($"[SkillEffectPool] WarmUp 완료 — 주소 수: {_warmUpAddresses.Count}, 인스턴스 수: {POOL_SIZE}개/주소");
    }

    public async UniTask AddCharactersAsync(IReadOnlyList<BaseCharacter> characters, CancellationToken cancellationToken = default)
    {
        HashSet<string> addresses = CollectAddresses(characters);

        foreach (string address in addresses)
        {
            if (string.IsNullOrEmpty(address))
                continue;
            if (_idle.ContainsKey(address))
                continue;

            Queue<GameObject> queue = new Queue<GameObject>(POOL_SIZE);
            List<GameObject> active = new List<GameObject>(POOL_SIZE);

            for (int i = 0; i < POOL_SIZE; i++)
            {
                AsyncOperationHandle<GameObject> handle = Addressables.InstantiateAsync(address);
                try
                {
                    GameObject instance = await handle.ToUniTask(cancellationToken: cancellationToken);
                    instance.SetActive(false);
                    instance.transform.localScale = Vector3.one * 10f;
                    foreach (SpriteRenderer sr in instance.GetComponentsInChildren<SpriteRenderer>(true))
                        sr.sortingOrder = 1;
                    Object.DontDestroyOnLoad(instance);
                    queue.Enqueue(instance);
                    _handles.Add(handle);
                }
                catch (System.Exception e)
                {
                    Debug.LogError($"[SkillEffectPool] 몬스터 스킬 프리팹 생성 실패 address={address}: {e.Message}");
                }
            }

            _idle[address] = queue;
            _active[address] = active;
            _warmUpAddresses.Add(address);
        }

        Debug.Log($"[SkillEffectPool] AddCharacters 완료 — 총 주소 수: {_warmUpAddresses.Count}");
    }

    public void Play(string prefabAddress, Transform slotTransform)
    {
        if (string.IsNullOrEmpty(prefabAddress))
            return;

        if (_idle.TryGetValue(prefabAddress, out Queue<GameObject> queue) == false)
        {
            Debug.LogWarning($"[SkillEffectPool] 풀에 없는 주소: {prefabAddress}. 현재 키 목록: {string.Join(", ", _idle.Keys)}");
            return;
        }

        if (queue.Count == 0)
        {
            Debug.LogWarning($"[SkillEffectPool] 풀 소진 address={prefabAddress}");
            return;
        }

        GameObject instance = queue.Dequeue();
        _active[prefabAddress].Add(instance);

        Vector3 slotPos = slotTransform.position;
        instance.transform.position = new Vector3(slotPos.x, slotPos.y, slotPos.z);
        instance.SetActive(true);
        Debug.Log($"[SkillEffectPool] {instance.name} 활성화 — worldPos={instance.transform.position}, parent={instance.transform.parent?.name}, slotWorldPos={slotTransform.position}");

        // 람다식: 반환 대상 주소·인스턴스를 클로저로 캡처하기 위해 사용한다
        ReturnAfterPlayAsync(prefabAddress, instance).Forget();
    }

    private async UniTaskVoid ReturnAfterPlayAsync(string prefabAddress, GameObject instance)
    {
        EffectSpritePlayer player = instance.GetComponent<EffectSpritePlayer>();
        if (player != null)
        {
            // 람다식: 단일 조건 폴링을 인라인으로 표현하기 위해 사용한다
            await UniTask.WaitUntil(
                () => player.enabled == false,
                cancellationToken: instance.GetCancellationTokenOnDestroy());
        }
        else
        {
            await UniTask.Delay(1000, cancellationToken: instance.GetCancellationTokenOnDestroy());
        }

        Return(prefabAddress, instance);
    }

    private void Return(string prefabAddress, GameObject instance)
    {
        if (instance == null)
            return;

        instance.SetActive(false);

        if (_active.TryGetValue(prefabAddress, out List<GameObject> activeList))
            activeList.Remove(instance);

        if (_idle.TryGetValue(prefabAddress, out Queue<GameObject> queue))
            queue.Enqueue(instance);
    }

    public void ReturnAll()
    {
        foreach (KeyValuePair<string, List<GameObject>> kvp in _active)
        {
            string address = kvp.Key;
            GameObject[] snapshot = kvp.Value.ToArray();
            foreach (GameObject instance in snapshot)
                Return(address, instance);
        }
    }

    public void Clear()
    {
        ReturnAll();

        foreach (KeyValuePair<string, Queue<GameObject>> kvp in _idle)
        {
            foreach (GameObject instance in kvp.Value)
            {
                if (instance != null)
                    Addressables.ReleaseInstance(instance);
            }
            kvp.Value.Clear();
        }

        _idle.Clear();
        _active.Clear();
        _handles.Clear();
    }

    private static HashSet<string> CollectAddresses(IReadOnlyList<BaseCharacter> party)
    {
        HashSet<string> result = new HashSet<string>();
        if (party == null)
            return result;

        foreach (BaseCharacter character in party)
        {
            if (character == null) continue;
            if (character.Skills == null) continue;

            foreach (SkillBase skill in character.Skills)
            {
                if (skill == null) continue;
                if (skill.Data == null) continue;

                string address = skill.Data.PrefabAddress;
                if (string.IsNullOrEmpty(address) == false)
                    result.Add(address);
            }
        }

        return result;
    }

    private static bool AddressSetEquals(HashSet<string> a, HashSet<string> b)
    {
        if (a.Count != b.Count)
            return false;
        foreach (string s in a)
        {
            if (b.Contains(s) == false)
                return false;
        }
        return true;
    }
}
