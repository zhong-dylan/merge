using UnityEngine;

public class GameLaunch : MonoBehaviour
{
    [SerializeField] private string configName = "Debug";
    [SerializeField] private string userName = "player_001";
    [SerializeField] private UILoginView loginPrefab;

    private UILoginView loginView;
    private LaunchConfig activeConfig;

    private System.Collections.IEnumerator Start()
    {
        var loader = GetComponent<Loader>();
        if (loader == null)
        {
            loader = gameObject.AddComponent<Loader>();
        }

        var configLoaded = false;
        loader.Load<LaunchConfig>($"LaunchConfigs/{configName}", config =>
        {
            activeConfig = config;
            configLoaded = true;
        });
        yield return new WaitUntil(() => configLoaded);

        if (activeConfig == null)
        {
            Debug.LogError($"Missing launch config: {configName}.", this);
            yield break;
        }

        Logger.SetEnable(activeConfig.EnableLogger);
        InitManagers();
        yield return LaunchCoroutine();
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
        Logger.Info("Launch step 1/5: load config.", this);
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

        Logger.Info("Launch step 2/5: init login view.", this);
        OpenLoginView();
        if (loginView == null)
        {
            yield break;
        }

        Logger.Info("Launch step 3/5: resolve server.", this);
        var config = activeConfig;
        var releaseConfig = ReleaseServerConfigResolver.ResolveByHost(
            config.Host,
            configName,
            config.ServerVersion,
            config.ServerKey,
            out var resolveError);
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
            $"Config={configName} version={releaseConfig.Version} ip={resolvedIp} game={releaseConfig.ServerAddress} admin={releaseConfig.AdminAddress}",
            this);
        UpdateLoginProgress(0.55f);

        Logger.Info("Launch step 4/5: load font.", this);
        yield return PlatformMgr.I.Platform.DownloadFont(OnFontLoaded, OnLoginFailed);
        UpdateLoginProgress(0.7f);

        Logger.Info("Launch step 5/5: login.", this);
        yield return NakamaModel.I.Login(selectedServer, userName, OnLoginSuccess, OnLoginFailed);
    }

    private void OpenLoginView()
    {
        loginView = UIMgr.I.OpenView(loginPrefab);
        if (loginView == null)
        {
            OnLoginFailed("Login prefab is missing on GameLaunch.");
            return;
        }

        UpdateLoginProgress(0.2f);
    }

    private void OnLoginSuccess(PlayerBootstrapResponse profile)
    {
        UpdateLoginProgress(1f);
        UIMgr.I.OpenView<UIMainView>(view =>
        {
            if (view == null)
            {
                Logger.Warn($"Main view not found in Addressables: {nameof(UIMainView)}", this);
                return;
            }

            UIMgr.I.CloseView<UILoginView>();
            loginView = null;
        });
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
}
