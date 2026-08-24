using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public sealed class FontMgr : MonoSingle<FontMgr>
{
    private Font downloadedFont;
    private TMP_FontAsset downloadedFontAsset;

    public event Action<TMP_FontAsset> FontAssetChanged;

    public Font DownloadedFont => downloadedFont;
    public TMP_FontAsset DownloadedFontAsset => downloadedFontAsset;
    public bool HasDownloadedFontAsset => downloadedFontAsset != null;

    public void RegisterDownloadedFont(Font font)
    {
        if (font == null)
        {
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
            return;
        }

        if (downloadedFontAsset == null)
        {
            Logger.Error($"Create TMP_FontAsset failed for font: {font.name}", this);
            return;
        }

        downloadedFontAsset.name = $"{font.name}_Runtime_TMP";
        NotifyFontReady(downloadedFontAsset);
    }

    private void NotifyFontReady(TMP_FontAsset fontAsset)
    {
        FontAssetChanged?.Invoke(fontAsset);
    }
}
