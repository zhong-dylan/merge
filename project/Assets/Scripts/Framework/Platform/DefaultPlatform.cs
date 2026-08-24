using System;
using System.Collections;
using UnityEngine;

public sealed class DefaultPlatform : IPlatform
{
    private readonly Loader loader;
    private readonly PlatformType platformType;

    public PlatformType PlatformType => platformType;

    public DefaultPlatform(Loader loader, PlatformType platformType)
    {
        this.loader = loader;
        this.platformType = platformType;
    }

    public IEnumerator DownloadFont(Action<Font> onCompleted, Action<string> onFailed)
    {
        Logger.Warn($"DownloadFont is using DefaultPlatform fallback for {platformType}.");

        if (loader == null)
        {
            onFailed?.Invoke("Platform loader is null.");
            yield break;
        }

        onFailed?.Invoke($"Platform font download is not implemented for {platformType}.");
    }
}
