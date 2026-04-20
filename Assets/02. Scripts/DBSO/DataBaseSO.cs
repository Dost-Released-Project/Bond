using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DataBaseSO", menuName = "Scriptable Objects/DataBaseSO")]
public class DataBaseSO : ScriptableObject
{
    [SerializeField] private List<BaseSO> _soList = new List<BaseSO>();

    private Dictionary<string, BaseSO> _cache;

    private void OnEnable() => BuildCache();

    private void BuildCache()
    {
        _cache = new Dictionary<string, BaseSO>(_soList.Count);
        foreach (var so in _soList)
        {
            if (so == null) continue;
            if (!_cache.TryAdd(so.Id, so))
                Debug.LogWarning($"[DataBaseSO] 중복 ID: {so.Id}", so);
        }
    }

    /// <summary>타입 캐스팅 포함 조회. 실패 시 null 반환.</summary>
    public T GetSO<T>(string id) where T : BaseSO
    {
        if (_cache == null) BuildCache();
        return _cache.TryGetValue(id, out var result) ? result as T : null;
    }

    /// <summary>타입 불필요한 경우용 오버로드.</summary>
    public BaseSO GetSO(string id) => GetSO<BaseSO>(id);

#if UNITY_EDITOR
    private void OnValidate() => BuildCache();
#endif
}