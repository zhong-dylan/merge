using System;
using System.Collections;
using UnityEngine;

public interface IPlatform
{
    PlatformType PlatformType { get; }
    IEnumerator DownloadFont(Action<Font> onCompleted, Action<string> onFailed);
}
