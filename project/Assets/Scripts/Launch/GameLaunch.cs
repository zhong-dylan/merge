using UnityEngine;

public class GameLaunch : MonoBehaviour
{
    [SerializeField] private LaunchConfig launchConfig;
    [SerializeField] private int selectedServerIndex;
    private UILoginView loginView;
    private bool isLoginViewReady;

    private void Start()
    {
        if (launchConfig == null)
        {
            Logger.Warn("LaunchConfig is not assigned.", this);
            return;
        }

        Logger.SetEnable(launchConfig.EnableLogger);
        selectedServerIndex = Mathf.Clamp(selectedServerIndex, 0, Mathf.Max(0, launchConfig.Servers.Count - 1));

        InitManagers();
        OpenLoginView();
        StartCoroutine(LaunchCoroutine());
    }

    private void InitManagers()
    {
        _ = TimeMgr.I;
        _ = PlayerPrefsMgr.I;
        _ = EventMgr.I;
        _ = PoolMgr.I;
        _ = AtlasMgr.I;
        _ = CfgMgr.I;
        _ = ModelMgr.I;
        _ = AssetsMgr.I;
        _ = AudioMgr.I;
        _ = UIMgr.I;
        _ = PlatformMgr.I;
        _ = FontMgr.I;
    }

    private System.Collections.IEnumerator LaunchCoroutine()
    {
        Logger.Info("Launch step 1/5: init.", this);
        yield return new WaitUntil(() => isLoginViewReady);

        Logger.Info("Launch step 2/5: load config.", this);
        var configLoaded = false;
        var configFailed = false;
        string configError = null;
        yield return CfgMgr.I.LoadAll(
            () => configLoaded = true,
            error =>
            {
                configError = error;
                configFailed = true;
            });

        if (configFailed || !configLoaded)
        {
            OnLoginFailed(configError ?? "Config load failed.");
            yield break;
        }

        Logger.Info("Launch step 3/5: resolve server.", this);
        var selectedEntry = GetSelectedServerEntry();
        var releaseConfig = ReleaseServerConfigResolver.ResolveByMode(selectedEntry, CfgMgr.I, out var resolveError);
        if (releaseConfig == null)
        {
            OnLoginFailed(resolveError);
            yield break;
        }

        var selectedServer = releaseConfig.ToServerOption();
        NakamaModel.I.SetServer(
            releaseConfig.Version,
            releaseConfig.ServerName,
            releaseConfig.ServerAddress,
            releaseConfig.ServerKey,
            releaseConfig.AdminAddress);
        var resolvedIp = ReleaseServerConfigResolver.TryExtractGameIp(releaseConfig);
        Logger.Info(
            $"Server={selectedEntry?.modeName} version={releaseConfig.Version} ip={resolvedIp} game={releaseConfig.ServerAddress} admin={releaseConfig.AdminAddress}",
            this);
        UpdateLoginProgress(0.55f);

        Logger.Info("Launch step 4/5: load font.", this);
        yield return PlatformMgr.I.Platform.DownloadFont(OnFontLoaded, OnLoginFailed);
        UpdateLoginProgress(0.7f);

        Logger.Info("Launch step 5/5: login.", this);
        yield return NakamaModel.I.Login(selectedServer, launchConfig.UserName, OnLoginSuccess, OnLoginFailed);
    }

    private void OpenLoginView()
    {
        isLoginViewReady = false;

        UIMgr.I.OpenView<UILoginView>(view =>
        {
            loginView = view;
            isLoginViewReady = true;

            if (view == null)
            {
                Logger.Warn($"Login view not found in Addressables: {nameof(UILoginView)}", this);
                return;
            }

            UpdateLoginProgress(0.2f);
        });
    }

    private void OnLoginSuccess(PlayerBootstrapResponse profile)
    {
        UpdateLoginProgress(1f);
    }

    private void OnFontLoaded(Font font)
    {
        if (font == null)
        {
            return;
        }

        FontMgr.I.RegisterDownloadedFont(font);
        Logger.Success($"Font loaded: {font.name}", this);
    }

    private void UpdateLoginProgress(float progress)
    {
        var currentLoginView = loginView ?? UIMgr.I.GetView<UILoginView>();
        if (currentLoginView == null)
        {
            return;
        }

        currentLoginView.SetProgress(progress);
    }

    private void OnLoginFailed(string error)
    {
        Logger.Error(error, this);

        var currentLoginView = loginView ?? UIMgr.I.GetView<UILoginView>();
        if (currentLoginView != null)
        {
            currentLoginView.SetProgress(0f);
        }
    }

    private LaunchServerEntry GetSelectedServerEntry()
    {
        var servers = launchConfig?.Servers;
        if (servers == null || servers.Count == 0)
        {
            return null;
        }

        var safeIndex = Mathf.Clamp(selectedServerIndex, 0, servers.Count - 1);
        return servers[safeIndex];
    }
}
