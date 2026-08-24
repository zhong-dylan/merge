using System;
using System.Collections.Generic;
using UnityEngine;

public sealed class PoolMgr : MonoSingle<PoolMgr>
{
    private const float DefaultIdleReleaseSeconds = 30f;

    private readonly Dictionary<int, PoolInfo> pools = new();
    private readonly Dictionary<GameObject, int> instancePoolIds = new();
    private readonly Dictionary<string, int> addressPoolIds = new();

    private Loader loader;
    private Transform cacheRoot;

    protected override void Init()
    {
        base.Init();
        EnsureLoader();
        EnsureCacheRoot();
    }

    private void Update()
    {
        CleanupIdlePools();
    }

    public GameObject Spawn(GameObject prefab, Transform parent = null)
    {
        if (prefab == null)
        {
            return null;
        }

        EnsureCacheRoot();
        var pool = GetOrCreatePool(prefab);
        return SpawnFromPool(pool, parent);
    }

    public void Spawn(string key, Action<GameObject> onCompleted, Transform parent = null)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            onCompleted?.Invoke(null);
            return;
        }

        EnsureLoader();
        EnsureCacheRoot();

        if (addressPoolIds.TryGetValue(key, out var existingPoolId) && pools.TryGetValue(existingPoolId, out var existingPool))
        {
            onCompleted?.Invoke(SpawnFromPool(existingPool, parent));
            return;
        }

        loader.Load<GameObject>(key, prefab =>
        {
            if (prefab == null)
            {
                Logger.Error($"Pool prefab load failed: {key}", this);
                onCompleted?.Invoke(null);
                return;
            }

            var pool = GetOrCreatePool(prefab, key);
            onCompleted?.Invoke(SpawnFromPool(pool, parent));
        });
    }

    public bool TrySpawn(string key, out GameObject instance, Transform parent = null)
    {
        instance = null;
        if (string.IsNullOrWhiteSpace(key))
        {
            return false;
        }

        EnsureCacheRoot();
        if (!addressPoolIds.TryGetValue(key, out var poolId) || !pools.TryGetValue(poolId, out var pool))
        {
            return false;
        }

        instance = SpawnFromPool(pool, parent);
        return instance != null;
    }

    public T SpawnComponent<T>(T prefab, Transform parent = null) where T : Component
    {
        if (prefab == null)
        {
            return null;
        }

        var instance = Spawn(prefab.gameObject, parent);
        return instance != null ? instance.GetComponent<T>() : null;
    }

    public void SpawnComponent<T>(string key, Action<T> onCompleted, Transform parent = null) where T : Component
    {
        Spawn(key, instance =>
        {
            onCompleted?.Invoke(instance != null ? instance.GetComponent<T>() : null);
        }, parent);
    }

    public bool TrySpawnComponent<T>(string key, out T component, Transform parent = null) where T : Component
    {
        component = null;
        if (!TrySpawn(key, out var instance, parent) || instance == null)
        {
            return false;
        }

        component = instance.GetComponent<T>();
        return component != null;
    }

    public void Despawn(GameObject instance)
    {
        if (instance == null)
        {
            return;
        }

        EnsureCacheRoot();

        if (!instancePoolIds.TryGetValue(instance, out var poolId) || !pools.TryGetValue(poolId, out var pool))
        {
            Destroy(instance);
            return;
        }

        instance.SetActive(false);
        instance.transform.SetParent(pool.Root, false);
        pool.CachedInstances.Enqueue(instance);
        pool.ActiveCount = Mathf.Max(0, pool.ActiveCount - 1);
        pool.LastUsedTime = Time.unscaledTime;
    }

    public void Despawn(Component component)
    {
        if (component == null)
        {
            return;
        }

        Despawn(component.gameObject);
    }

    public Coroutine Despawn(GameObject instance, float delaySeconds)
    {
        if (delaySeconds <= 0f)
        {
            Despawn(instance);
            return null;
        }

        return TimeMgr.I.DelayCall(delaySeconds, () => Despawn(instance));
    }

    public void Prewarm(GameObject prefab, int count)
    {
        if (prefab == null || count <= 0)
        {
            return;
        }

        var tempInstances = new List<GameObject>(count);
        for (var i = 0; i < count; i++)
        {
            var instance = Spawn(prefab, cacheRoot);
            if (instance != null)
            {
                tempInstances.Add(instance);
            }
        }

        for (var i = 0; i < tempInstances.Count; i++)
        {
            Despawn(tempInstances[i]);
        }
    }

    public void Prewarm(string key, int count, Action onCompleted = null)
    {
        if (string.IsNullOrWhiteSpace(key) || count <= 0)
        {
            onCompleted?.Invoke();
            return;
        }

        Spawn(key, instance =>
        {
            if (instance == null)
            {
                onCompleted?.Invoke();
                return;
            }

            if (!instancePoolIds.TryGetValue(instance, out var poolId) || !pools.TryGetValue(poolId, out var pool))
            {
                Despawn(instance);
                onCompleted?.Invoke();
                return;
            }

            var tempInstances = new List<GameObject> { instance };
            for (var i = 1; i < count; i++)
            {
                var spawned = Spawn(pool.Prefab, cacheRoot);
                if (spawned != null)
                {
                    tempInstances.Add(spawned);
                }
            }

            for (var i = 0; i < tempInstances.Count; i++)
            {
                Despawn(tempInstances[i]);
            }

            onCompleted?.Invoke();
        }, cacheRoot);
    }

    private void EnsureLoader()
    {
        if (loader == null)
        {
            loader = GetComponent<Loader>();
            if (loader == null)
            {
                loader = gameObject.AddComponent<Loader>();
            }
        }
    }

    private void EnsureCacheRoot()
    {
        if (cacheRoot != null)
        {
            return;
        }

        var rootTransform = transform.Find("PoolRoot");
        if (rootTransform == null)
        {
            var rootObject = new GameObject("PoolRoot");
            rootObject.transform.SetParent(transform, false);
            cacheRoot = rootObject.transform;
        }
        else
        {
            cacheRoot = rootTransform;
        }
    }

    private PoolInfo GetOrCreatePool(GameObject prefab, string addressKey = null)
    {
        var poolId = prefab.GetInstanceID();
        if (pools.TryGetValue(poolId, out var pool))
        {
            pool.LastUsedTime = Time.unscaledTime;
            if (!string.IsNullOrWhiteSpace(addressKey))
            {
                addressPoolIds[addressKey] = poolId;
                pool.AddressKey = addressKey;
            }

            return pool;
        }

        var rootObject = new GameObject($"{prefab.name}_Pool");
        rootObject.transform.SetParent(cacheRoot, false);

        pool = new PoolInfo
        {
            PoolId = poolId,
            Prefab = prefab,
            AddressKey = addressKey,
            Root = rootObject.transform,
            CachedInstances = new Queue<GameObject>(),
            LastUsedTime = Time.unscaledTime,
        };

        pools[poolId] = pool;
        if (!string.IsNullOrWhiteSpace(addressKey))
        {
            addressPoolIds[addressKey] = poolId;
        }

        return pool;
    }

    private GameObject SpawnFromPool(PoolInfo pool, Transform parent)
    {
        if (pool == null || pool.Prefab == null)
        {
            return null;
        }

        GameObject instance = null;
        while (pool.CachedInstances.Count > 0 && instance == null)
        {
            instance = pool.CachedInstances.Dequeue();
        }

        if (instance == null)
        {
            instance = Instantiate(pool.Prefab);
        }

        pool.ActiveCount++;
        pool.LastUsedTime = Time.unscaledTime;
        instancePoolIds[instance] = pool.PoolId;
        instance.transform.SetParent(parent, false);
        instance.SetActive(true);
        return instance;
    }

    private void CleanupIdlePools()
    {
        if (pools.Count == 0)
        {
            return;
        }

        var now = Time.unscaledTime;
        var releasePoolIds = ListPoolIdsToRelease(now);
        for (var i = 0; i < releasePoolIds.Count; i++)
        {
            ReleasePool(releasePoolIds[i]);
        }
    }

    private List<int> ListPoolIdsToRelease(float now)
    {
        var result = new List<int>();
        foreach (var pair in pools)
        {
            var pool = pair.Value;
            if (pool == null || pool.ActiveCount > 0)
            {
                continue;
            }

            if (now - pool.LastUsedTime < DefaultIdleReleaseSeconds)
            {
                continue;
            }

            result.Add(pair.Key);
        }

        return result;
    }

    private void ReleasePool(int poolId)
    {
        if (!pools.TryGetValue(poolId, out var pool))
        {
            return;
        }

        while (pool.CachedInstances.Count > 0)
        {
            var instance = pool.CachedInstances.Dequeue();
            if (instance == null)
            {
                continue;
            }

            instancePoolIds.Remove(instance);
            Destroy(instance);
        }

        if (!string.IsNullOrWhiteSpace(pool.AddressKey))
        {
            addressPoolIds.Remove(pool.AddressKey);
            loader?.Unload(pool.AddressKey);
        }

        if (pool.Root != null)
        {
            Destroy(pool.Root.gameObject);
        }

        pools.Remove(poolId);
    }

    private sealed class PoolInfo
    {
        public int PoolId;
        public GameObject Prefab;
        public string AddressKey;
        public Transform Root;
        public Queue<GameObject> CachedInstances;
        public int ActiveCount;
        public float LastUsedTime;
    }
}
