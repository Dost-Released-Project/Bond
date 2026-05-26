using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

/// <summary>
/// 모든 DataBaseSO 에셋을 중앙에서 관리하는 정적 레지스트리.<br/>
/// 다른 시스템은 SO가 필요할 때 더 이상 Addressables를 직접 호출하지 않고
/// 이 클래스만 통해 접근한다.
///<para/>
/// 식별자<br/>
///   실제 키는 Addressable 키(string)다. 타입은 어디까지나 컨벤션 단축형으로만 쓰인다.<br/>
///   같은 타입의 DB가 여러 개 있거나 raw DataBaseSO로 만든 에셋도 정상 관리된다.
///<para/>
/// 컨벤션 (T → 키 매핑)<br/>
///   Addressable 키 = 클래스명에서 "SO" 접미사 제거.<br/>
///   예) SkillDataBaseSO → "SkillDataBase".<br/>
///   이 매핑은 단축형 API(Get&lt;T&gt;(), LoadAsync&lt;T&gt;() 등)에서만 사용된다.
///<para/>
/// 권장 운용<br/>
///   1) 게임 시작 시 PreloadAsync로 필요한 DB들을 일괄 로드.<br/>
///   2) 이후 어디서나 DBSORegistry.GetSO&lt;TSO&gt;(id) 한 줄로 SO 조회.
///<para/>
/// 사용 예
/// <code>
///   // [1] 부트스트랩 — 한 번만 (Addressable 키 직접 지정)
///   await DBSORegistry.PreloadAsync(
///       "SkillDataBase",
///       "ClassDataBase",
///       "ConsumableDataBase",
///       "AccessoryDataBase");
///
///   // [2] SO 조회 — 어느 DB인지 신경 쓸 필요 없음
///   SkillData skill = DBSORegistry.GetSO&lt;SkillData&gt;(skillId);
///   BaseItem  item  = DBSORegistry.GetSO&lt;BaseItem&gt;(itemId);   // Consumable/Accessory 자동 탐색
///
///   // [3] DB 인스턴스 자체가 필요한 경우 — 컨벤션 (TypeName-"SO")
///   var skillDb = DBSORegistry.GetDb&lt;SkillDataBaseSO&gt;();
///
///   // [4] 같은 타입의 DB가 여러 개거나 raw DataBaseSO일 때 — 키 명시
///   var heroes  = DBSORegistry.GetDb&lt;ClassDataBaseSO&gt;("HeroClassDB");
///   var villains= DBSORegistry.GetDb&lt;ClassDataBaseSO&gt;("VillainClassDB");
/// </code>
/// </summary>
public static class DBSORegistry
{
    // Addressable 키 → 인스턴스. 키가 진짜 식별자다.
    private static readonly Dictionary<string, DataBaseSO>          _databases = new Dictionary<string, DataBaseSO>();
    private static readonly Dictionary<string, AsyncOperationHandle> _handles  = new Dictionary<string, AsyncOperationHandle>();
    private static readonly Dictionary<string, Task<DataBaseSO>>     _pending  = new Dictionary<string, Task<DataBaseSO>>();

    /// <summary>현재 로드/등록된 모든 DataBaseSO 인스턴스.</summary>
    public static IReadOnlyCollection<DataBaseSO> All => _databases.Values;

    /// <summary>현재 로드/등록된 모든 키.</summary>
    public static IReadOnlyCollection<string> LoadedKeys => _databases.Keys;

    // ── (1) SO 조회 — 일반 시스템이 호출하는 주 API ─────────────────────────

    /// <summary>
    /// ID로 SO를 가져온다. 어느 DB에 들어 있는지 알 필요 없다.
    /// 등록된 모든 DB를 순회하여 첫 매치를 반환.
    /// 호출 전에 해당 DB가 로드(또는 PreloadAsync로 사전 로드)되어 있어야 한다.
    /// </summary>
    public static T GetSO<T>(string id) where T : BaseSO
    {
        if (string.IsNullOrEmpty(id)) return null;
        foreach (var db in _databases.Values)
        {
            var so = db.GetSO<T>(id);
            if (so != null) return so;
        }
        return null;
    }

    /// <summary>등록된 모든 DB를 가로질러 조건에 맞는 SO들을 yield 반환.</summary>
    public static IEnumerable<T> QuerySO<T>(Func<T, bool> predicate) where T : BaseSO
    {
        if (predicate == null) yield break;
        foreach (var db in _databases.Values)
        {
            foreach (var so in db.Query(predicate))
                yield return so;
        }
    }

    // ── (2) DB 조회 — 키 명시 (일반형) ─────────────────────────────────

    /// <summary>해당 키의 DB가 이미 로드되었는지 여부.</summary>
    public static bool IsLoaded(string key)
        => !string.IsNullOrEmpty(key) && _databases.ContainsKey(key);

    /// <summary>키로 DB 인스턴스 조회. 캐스팅 실패/미로드 시 null.</summary>
    public static T GetDb<T>(string key) where T : DataBaseSO
        => (string.IsNullOrEmpty(key) ? null : _databases.GetValueOrDefault(key)) as T;

    /// <summary>키로 DB 인스턴스 조회 (베이스 타입).</summary>
    public static DataBaseSO GetDb(string key)
        => string.IsNullOrEmpty(key) ? null : _databases.GetValueOrDefault(key);

    // ── (3) DB 조회 — 컨벤션 단축형 (T → 키 자동 매핑) ───────────────────

    /// <summary>
    /// 컨벤션 키로 DB 인스턴스 조회. Addressable 키 = 클래스명 - "SO".
    /// 한 타입에 DB가 하나뿐인 일반 케이스에서 단축형으로 쓴다.
    /// </summary>
    public static T GetDb<T>() where T : DataBaseSO => GetDb<T>(ResolveKey<T>());

    /// <summary>컨벤션 키로 로드 여부 확인.</summary>
    public static bool IsLoaded<T>() where T : DataBaseSO => IsLoaded(ResolveKey<T>());

    // ── (4) 로드 ─────────────────────────────────────────────────────

    /// <summary>비동기 로드 (키 명시). 이미 로드된 경우 즉시 반환, 진행 중이면 동일 Task 공유.</summary>
    public static async Task<T> LoadAsync<T>(string key) where T : DataBaseSO
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[DBSORegistry] LoadAsync: 키가 비어 있다.");
            return null;
        }

        if (_databases.TryGetValue(key, out var cached)) return cached as T;

        if (_pending.TryGetValue(key, out var pending))
            return (await pending) as T;

        var task = LoadInternalAsync<T>(key);
        _pending[key] = task;
        try { return (await task) as T; }
        finally { _pending.Remove(key); }
    }

    /// <summary>비동기 로드 (컨벤션 키).</summary>
    public static Task<T> LoadAsync<T>() where T : DataBaseSO
        => LoadAsync<T>(ResolveKey<T>());

    /// <summary>동기 로드 (키 명시). Awake/Start처럼 즉시 결과가 필요할 때만.</summary>
    public static T LoadSync<T>(string key) where T : DataBaseSO
    {
        if (string.IsNullOrEmpty(key))
        {
            Debug.LogError("[DBSORegistry] LoadSync: 키가 비어 있다.");
            return null;
        }

        if (_databases.TryGetValue(key, out var cached)) return cached as T;

        var handle = Addressables.LoadAssetAsync<T>(key);
        var asset  = handle.WaitForCompletion();
        Store(key, asset, handle);
        return asset;
    }

    /// <summary>동기 로드 (컨벤션 키).</summary>
    public static T LoadSync<T>() where T : DataBaseSO => LoadSync<T>(ResolveKey<T>());

    /// <summary>
    /// 여러 키를 병렬로 사전 로드한다. 부트스트랩에서 한 번 호출.
    /// 베이스 타입(DataBaseSO)으로 로드하지만 실제 인스턴스는 서브클래스가 그대로 들어온다.
    /// 이후 Get&lt;T&gt;(key)로 캐스팅해서 꺼내면 된다.
    ///<para/>
    /// 주의: 절대 .Wait() / .Result로 받지 말 것. 메인 스레드 데드락이 발생한다.
    /// 동기 경로가 필요하면 PreloadSync를 사용한다.
    /// </summary>
    public static async Task PreloadAsync(params string[] keys)
    {
        if (keys == null || keys.Length == 0) return;

        var tasks = new List<Task>(keys.Length);
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key)) continue;
            if (_databases.ContainsKey(key)) continue;
            if (_pending.ContainsKey(key))
            {
                tasks.Add(_pending[key]);
                continue;
            }
            tasks.Add(LoadAsync<DataBaseSO>(key));
        }
        if (tasks.Count > 0)
            await Task.WhenAll(tasks);
    }

    /// <summary>
    /// 여러 키를 동기적으로 사전 로드한다. Awake/Start처럼 즉시 결과가 필요할 때 사용.
    /// 모든 핸들을 먼저 생성(병렬 시작)한 뒤 차례로 WaitForCompletion을 호출하므로
    /// 가능한 한 병렬화된다. 메인 스레드를 잠시 블록한다.
    /// </summary>
    public static void PreloadSync(params string[] keys)
    {
        if (keys == null || keys.Length == 0) return;

        var inFlight = new List<(string key, AsyncOperationHandle<DataBaseSO> handle)>(keys.Length);
        foreach (var key in keys)
        {
            if (string.IsNullOrEmpty(key)) continue;
            if (_databases.ContainsKey(key)) continue;
            inFlight.Add((key, Addressables.LoadAssetAsync<DataBaseSO>(key)));
        }

        foreach (var (key, handle) in inFlight)
        {
            var asset = handle.WaitForCompletion();
            Store(key, asset, handle);
        }
    }

    // ── (5) 외부 주입 — RootScope에서 Inspector 참조 SO를 등록 ─────────────

    /// <summary>외부에서 미리 확보한 SO를 키와 함께 등록한다.</summary>
    public static void Register(string key, DataBaseSO database)
    {
        if (string.IsNullOrEmpty(key) || database == null) return;

        // 같은 키에 이전 Addressable 핸들이 남아 있으면 누수 방지를 위해 해제.
        if (_handles.TryGetValue(key, out var oldHandle))
        {
            if (oldHandle.IsValid()) Addressables.Release(oldHandle);
            _handles.Remove(key);
        }
        _databases[key] = database;
    }

    /// <summary>컨벤션 키로 등록 (한 타입 = 한 DB인 일반 케이스).</summary>
    public static void Register<T>(T database) where T : DataBaseSO
        => Register(ResolveKey<T>(), database);

    /// <summary>등록된 모든 핸들 해제 및 캐시 정리.</summary>
    public static void ReleaseAll()
    {
        foreach (var h in _handles.Values)
            if (h.IsValid()) Addressables.Release(h);
        _databases.Clear();
        _handles.Clear();
        _pending.Clear();
    }

    /// <summary>특정 키만 해제.</summary>
    public static void Release(string key)
    {
        if (string.IsNullOrEmpty(key)) return;
        if (_handles.TryGetValue(key, out var h))
        {
            if (h.IsValid()) Addressables.Release(h);
            _handles.Remove(key);
        }
        _databases.Remove(key);
        _pending.Remove(key);
    }

    // ── 내부 ──────────────────────────────────────────────────

    private static async Task<DataBaseSO> LoadInternalAsync<T>(string key) where T : DataBaseSO
    {
        var handle = Addressables.LoadAssetAsync<T>(key);
        var asset  = await handle.Task;
        Store(key, asset, handle);
        return asset;
    }

    private static void Store<T>(string key, T asset, AsyncOperationHandle handle) where T : DataBaseSO
    {
        if (asset == null)
        {
            Debug.LogError($"[DBSORegistry] '{key}' 로드 실패 ({typeof(T).Name}).");
            if (handle.IsValid()) Addressables.Release(handle);
            return;
        }
        _databases[key] = asset;
        _handles[key]   = handle;
    }

    private static string ResolveKey<T>() where T : DataBaseSO
    {
        var name = typeof(T).Name;
        return name.EndsWith("SO") ? name.Substring(0, name.Length - 2) : name;
    }

#if UNITY_EDITOR
    // 플레이 모드 종료 시 핸들을 해제해 도메인 리로드 비활성화 환경의 누수를 막는다.
    [UnityEditor.InitializeOnLoadMethod]
    private static void RegisterEditorHooks()
    {
        UnityEditor.EditorApplication.playModeStateChanged += state =>
        {
            if (state == UnityEditor.PlayModeStateChange.ExitingPlayMode)
                ReleaseAll();
        };
    }
#endif
}