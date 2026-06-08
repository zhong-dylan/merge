using System;
using UnityEngine;

public class Loader : MonoBehaviour
{
    public void Load<T>(string key, Action<T> onCompleted) where T : UnityEngine.Object
    {
        AssetsMgr.I.LoadAssetAsync(this, key, onCompleted);
    }

    public void Unload(string key)
    {
        AssetsMgr.I.ReleaseAsset(this, key);
    }

    private void OnDestroy()
    {
        AssetsMgr.I.ReleaseAll(this);
    }
}
