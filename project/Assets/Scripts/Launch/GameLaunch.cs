using UnityEngine;

public class GameLaunch : MonoBehaviour
{
    [SerializeField] private LaunchConfig launchConfig;
    private LoginView loginView;
    private bool isLoginViewReady;

    private void Start()
    {
        if (launchConfig == null)
        {
            Logger.Warn("LaunchConfig is not assigned.", this);
            return;
        }

        Logger.SetEnable(launchConfig.EnableLogger);

        var selectedServer = launchConfig.SelectedServer;
        if (selectedServer == null)
        {
            Logger.Warn("LaunchConfig does not contain any server options.", this);
            return;
        }

        InitManagers();
        OpenLoginView();
        StartCoroutine(LaunchCoroutine(selectedServer));
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

    private System.Collections.IEnumerator LaunchCoroutine(ServerOption selectedServer)
    {
        Logger.Info("Launch step 1/5: init.", this);
        yield return new WaitUntil(() => isLoginViewReady);

        Logger.Info("Launch step 2/5: get server config.", this);
        NakamaModel.I.SetServer(selectedServer.DisplayName, selectedServer.Address, launchConfig.ServerKey);
        UpdateLoginProgress(0.4f);
        yield return null;

        Logger.Info("Launch step 3/5: load config.", this);
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

        UIMgr.I.OpenView<LoginView>(UILayer.Login, view =>
        {
            loginView = view;
            isLoginViewReady = true;

            if (view == null)
            {
                Logger.Warn($"Login view not found in Addressables: {nameof(LoginView)}", this);
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
        var currentLoginView = loginView ?? UIMgr.I.GetView<LoginView>();
        if (currentLoginView == null)
        {
            return;
        }

        currentLoginView.SetProgress(progress);
    }

    private void OnLoginFailed(string error)
    {
        Logger.Error(error, this);

        var currentLoginView = loginView ?? UIMgr.I.GetView<LoginView>();
        if (currentLoginView != null)
        {
            currentLoginView.SetProgress(0f);
        }
    }
}
