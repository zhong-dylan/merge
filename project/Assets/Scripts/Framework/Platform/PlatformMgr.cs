public sealed class PlatformMgr : MonoSingle<PlatformMgr>
{
    public PlatformType CurrentPlatformType { get; private set; }
    public IPlatform Platform { get; private set; }
    public Loader Loader { get; private set; }

    protected override void Init()
    {
        base.Init();
        Loader = GetComponent<Loader>();
        if (Loader == null)
        {
            Loader = gameObject.AddComponent<Loader>();
        }

        CurrentPlatformType = DetectPlatformType();
        Platform = CreatePlatform(CurrentPlatformType, Loader);
        Logger.Info($"PlatformMgr initialized: {CurrentPlatformType}");
    }

    private static IPlatform CreatePlatform(PlatformType platformType, Loader loader)
    {
        return platformType switch
        {
            PlatformType.Editor => new EditorPlatform(loader),
            _ => new DefaultPlatform(loader, platformType),
        };
    }

    private static PlatformType DetectPlatformType()
    {
#if UNITY_EDITOR
        return PlatformType.Editor;
#elif UNITY_ANDROID
        return PlatformType.Android;
#elif UNITY_IOS
        return PlatformType.IOS;
#elif UNITY_STANDALONE_WIN
        return PlatformType.Windows;
#elif UNITY_STANDALONE_OSX
        return PlatformType.MacOS;
#elif UNITY_WEBGL
        return PlatformType.WebGL;
#else
        return PlatformType.Unknown;
#endif
    }
}
