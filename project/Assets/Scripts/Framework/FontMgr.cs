using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class FontMgr : MonoSingle<FontMgr>
{
    private Loader loader;
    private readonly List<Action<TMP_FontAsset>> pendingCallbacks = new();
    private Font downloadedFont;
    private TMP_FontAsset downloadedFontAsset;
    private bool isLoading;

    public event Action<TMP_FontAsset> FontAssetChanged;

    public Font DownloadedFont => downloadedFont;
    public TMP_FontAsset DownloadedFontAsset => downloadedFontAsset;
    public bool HasDownloadedFontAsset => downloadedFontAsset != null;

    protected override void Init()
    {
        base.Init();
        EnsureLoader();
    }

    public void EnsureFontAsset(Action<TMP_FontAsset> onCompleted, Action<string> onFailed = null)
    {
        if (downloadedFontAsset != null)
        {
            onCompleted?.Invoke(downloadedFontAsset);
            return;
        }

        if (onCompleted != null)
        {
            pendingCallbacks.Add(onCompleted);
        }

        if (isLoading)
        {
            return;
        }

        EnsureLoader();
        isLoading = true;
        StartCoroutine(DownloadFontCoroutine(onFailed));
    }

    public void RegisterDownloadedFont(Font font)
    {
        if (font == null)
        {
            NotifyPendingCallbacks(null);
            return;
        }

        downloadedFont = font;

        if (downloadedFontAsset != null && downloadedFontAsset.sourceFontFile == font)
        {
            NotifyFontReady(downloadedFontAsset);
            return;
        }

        if (downloadedFontAsset != null)
        {
            Destroy(downloadedFontAsset);
            downloadedFontAsset = null;
        }

        try
        {
            downloadedFontAsset = TMP_FontAsset.CreateFontAsset(font);
        }
        catch (Exception exception)
        {
            Logger.Error($"Create TMP_FontAsset failed: {exception.Message}", this);
            NotifyPendingCallbacks(null);
            return;
        }

        if (downloadedFontAsset == null)
        {
            Logger.Error($"Create TMP_FontAsset failed for font: {font.name}", this);
            NotifyPendingCallbacks(null);
            return;
        }

        downloadedFontAsset.name = $"{font.name}_Runtime_TMP";
        NotifyFontReady(downloadedFontAsset);
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

    private IEnumerator DownloadFontCoroutine(Action<string> onFailed)
    {
        var isDone = false;
        string error = null;

        yield return PlatformMgr.I.Platform.DownloadFont(font =>
        {
            RegisterDownloadedFont(font);
            isDone = true;
        }, message =>
        {
            error = message;
            isDone = true;
        });

        yield return new WaitUntil(() => isDone);

        isLoading = false;
        if (downloadedFontAsset != null)
        {
            yield break;
        }

        onFailed?.Invoke(error ?? "Font download failed.");
        NotifyPendingCallbacks(null);
    }

    private void NotifyFontReady(TMP_FontAsset fontAsset)
    {
        FontAssetChanged?.Invoke(fontAsset);
        NotifyPendingCallbacks(fontAsset);
    }

    private void NotifyPendingCallbacks(TMP_FontAsset fontAsset)
    {
        for (var i = 0; i < pendingCallbacks.Count; i++)
        {
            pendingCallbacks[i]?.Invoke(fontAsset);
        }

        pendingCallbacks.Clear();
    }
}
