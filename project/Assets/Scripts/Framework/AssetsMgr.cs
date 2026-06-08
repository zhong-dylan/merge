using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;

public sealed class AssetsMgr : MonoSingle<AssetsMgr>
{
    private readonly Dictionary<Loader, HashSet<string>> loaderAssetKeys = new();
    private readonly Dictionary<string, AssetHandleInfo> assetHandles = new();

    public Coroutine LoadAssetAsync<T>(Loader loader, string key, Action<T> onCompleted) where T : UnityEngine.Object
    {
        if (loader == null)
        {
            throw new ArgumentNullException(nameof(loader));
        }

        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Asset key cannot be null or empty.", nameof(key));
        }

        return StartCoroutine(LoadAssetCoroutine(loader, key, onCompleted));
    }

    public void ReleaseAsset(Loader loader, string key)
    {
        if (loader == null || string.IsNullOrWhiteSpace(key))
        {
            return;
        }

        if (!loaderAssetKeys.TryGetValue(loader, out var keys) || !keys.Remove(key))
        {
            return;
        }

        if (keys.Count == 0)
        {
            loaderAssetKeys.Remove(loader);
        }

        if (!assetHandles.TryGetValue(key, out var handleInfo))
        {
            return;
        }

        var nextRefCount = handleInfo.RefCount - 1;
        if (nextRefCount > 0)
        {
            assetHandles[key] = handleInfo.WithRefCount(nextRefCount);
            return;
        }

        Addressables.Release(handleInfo.Handle);
        assetHandles.Remove(key);
    }

    public void ReleaseAll(Loader loader)
    {
        if (loader == null || !loaderAssetKeys.TryGetValue(loader, out var keys))
        {
            return;
        }

        var releaseKeys = new List<string>(keys);
        foreach (var key in releaseKeys)
        {
            ReleaseAsset(loader, key);
        }
    }

    private IEnumerator LoadAssetCoroutine<T>(Loader loader, string key, Action<T> onCompleted) where T : UnityEngine.Object
    {
        var alreadyOwnedByLoader = HasLoaderKey(loader, key);

        if (assetHandles.TryGetValue(key, out var existingHandleInfo))
        {
            yield return existingHandleInfo.Handle;

            if (!alreadyOwnedByLoader)
            {
                RegisterLoaderKey(loader, key);
                assetHandles[key] = existingHandleInfo.WithRefCount(existingHandleInfo.RefCount + 1);
            }

            onCompleted?.Invoke(existingHandleInfo.Handle.Result as T);
            yield break;
        }

        var handle = Addressables.LoadAssetAsync<T>(key);
        yield return handle;

        if (handle.Status != AsyncOperationStatus.Succeeded)
        {
            Logger.Error($"Addressables load failed: {key}");
            onCompleted?.Invoke(null);
            yield break;
        }

        assetHandles[key] = new AssetHandleInfo(handle, 1);
        RegisterLoaderKey(loader, key);
        onCompleted?.Invoke(handle.Result);
    }

    private void RegisterLoaderKey(Loader loader, string key)
    {
        if (!loaderAssetKeys.TryGetValue(loader, out var keys))
        {
            keys = new HashSet<string>();
            loaderAssetKeys[loader] = keys;
        }

        keys.Add(key);
    }

    private bool HasLoaderKey(Loader loader, string key)
    {
        return loaderAssetKeys.TryGetValue(loader, out var keys) && keys.Contains(key);
    }

    private readonly struct AssetHandleInfo
    {
        public readonly AsyncOperationHandle Handle;
        public readonly int RefCount;

        public AssetHandleInfo(AsyncOperationHandle handle, int refCount)
        {
            Handle = handle;
            RefCount = refCount;
        }

        public AssetHandleInfo WithRefCount(int refCount)
        {
            return new AssetHandleInfo(Handle, refCount);
        }
    }
}
