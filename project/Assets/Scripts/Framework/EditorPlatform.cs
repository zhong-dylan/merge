using System;
using System.Collections;
using UnityEngine;

public sealed class EditorPlatform : IPlatform
{
    private const string DefaultFontAddress = "Fonts/fzcy_remote";
    private readonly Loader loader;

    public PlatformType PlatformType => PlatformType.Editor;

    public EditorPlatform(Loader loader)
    {
        this.loader = loader;
    }

    public IEnumerator DownloadFont(Action<Font> onCompleted, Action<string> onFailed)
    {
        if (loader == null)
        {
            onFailed?.Invoke("Platform loader is null.");
            yield break;
        }

        var isDone = false;
        Font fontResult = null;
        string errorResult = null;

        loader.Load<Font>(DefaultFontAddress, font =>
        {
            fontResult = font;
            if (font == null)
            {
                errorResult = $"Font load failed: {DefaultFontAddress}";
            }

            isDone = true;
        });

        yield return new WaitUntil(() => isDone);

        if (fontResult == null)
        {
            onFailed?.Invoke(errorResult ?? "Font load failed.");
            yield break;
        }

        onCompleted?.Invoke(fontResult);
    }
}
