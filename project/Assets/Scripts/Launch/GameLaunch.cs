using UnityEngine;

public class GameLaunch : MonoBehaviour
{
    [SerializeField] private bool enableLogger = true;
    [SerializeField] private string userName = "player_001";
    [SerializeField] private LaunchServerMode selectedServerMode = LaunchServerMode.Local;
    [SerializeField] private int serverVersion = 101;
    [SerializeField] private string localHost = "http://127.0.0.1";
    [SerializeField] private string devHost = "http://127.0.0.1";
    [SerializeField] private string releaseHost = "http://127.0.0.1";
    [SerializeField] private string serverKey = "local_socket_server_key_change_me";
    private UILoginView loginView;
    private bool isLoginViewReady;

    private void Start()
    {
        Logger.SetEnable(enableLogger);
        InitManagers();
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
        yield return new WaitUntil(() => isLoginViewReady);

        Logger.Info("Launch step 3/5: resolve server.", this);
        var releaseConfig = ReleaseServerConfigResolver.ResolveByMode(
            selectedServerMode,
            serverVersion,
            localHost,
            devHost,
            releaseHost,
            serverKey,
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
            $"Server={selectedServerMode} version={releaseConfig.Version} ip={resolvedIp} game={releaseConfig.ServerAddress} admin={releaseConfig.AdminAddress}",
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
