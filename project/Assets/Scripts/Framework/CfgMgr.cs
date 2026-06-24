using System;
using System.Collections;
using System.Collections.Generic;
using cfg;
using cfg.global;
using cfg.items;
using Luban.SimpleJSON;
using UnityEngine;

public sealed class CfgMgr : MonoSingle<CfgMgr>
{
    private static readonly string[] JsonTableKeys =
    {
        "Config/global_tbconfig_remote",
        "Config/items_tbitem_remote",
    };

    private readonly Dictionary<string, TextAsset> loadedJsonAssets = new();
    private Loader loader;

    public Tables Tables { get; private set; }
    public bool IsLoaded => Tables != null;

    protected override void Init()
    {
        base.Init();
        EnsureLoader();
    }

    public Coroutine LoadAll(Action onCompleted, Action<string> onFailed)
    {
        EnsureLoader();
        return TimeMgr.I.RunCoroutine(LoadAllCoroutine(onCompleted, onFailed));
    }

    public Tables GetTables()
    {
        return Tables;
    }

    public Tbconfig GetGlobalConfigTable()
    {
        return Tables?.Tbconfig;
    }

    public config GetGlobalConfig(string key)
    {
        return Tables?.Tbconfig?.GetOrDefault(key);
    }

    public string GetGlobalConfigValue(string key, string defaultValue = "")
    {
        return GetGlobalConfig(key)?.Value ?? defaultValue;
    }

    public IReadOnlyDictionary<string, config> GetGlobalConfigMap()
    {
        return Tables?.Tbconfig?.DataMap;
    }

    public Tbitem GetItemTable()
    {
        return Tables?.Tbitem;
    }

    public item GetItem(int id)
    {
        return Tables?.Tbitem?.GetOrDefault(id);
    }

    public IReadOnlyList<item> GetItems()
    {
        return Tables?.Tbitem?.DataList;
    }

    public IReadOnlyDictionary<int, item> GetItemMap()
    {
        return Tables?.Tbitem?.DataMap;
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

    private IEnumerator LoadAllCoroutine(Action onCompleted, Action<string> onFailed)
    {
        if (IsLoaded)
        {
            onCompleted?.Invoke();
            yield break;
        }

        loadedJsonAssets.Clear();
        for (var i = 0; i < JsonTableKeys.Length; i++)
        {
            var isDone = false;
            TextAsset loadedAsset = null;

            loader.Load<TextAsset>(JsonTableKeys[i], asset =>
            {
                loadedAsset = asset;
                isDone = true;
            });

            yield return new WaitUntil(() => isDone);

            if (loadedAsset == null)
            {
                onFailed?.Invoke($"Config load failed: {JsonTableKeys[i]}");
                yield break;
            }

            loadedJsonAssets[JsonTableKeys[i]] = loadedAsset;
        }

        try
        {
            Tables = new Tables(LoadJsonNode);
        }
        catch (Exception exception)
        {
            onFailed?.Invoke($"Config parse failed: {exception.Message}");
            yield break;
        }

        onCompleted?.Invoke();
    }

    private JSONNode LoadJsonNode(string tableName)
    {
        var assetKey = $"Config/{tableName}_remote";
        if (!loadedJsonAssets.TryGetValue(assetKey, out var textAsset) || textAsset == null)
        {
            throw new InvalidOperationException($"Config asset not loaded: {assetKey}");
        }

        return JSON.Parse(textAsset.text);
    }
}
