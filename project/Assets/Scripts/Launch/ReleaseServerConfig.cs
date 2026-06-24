using System;

public sealed class ReleaseServerConfig
{
    public string Version { get; set; }
    public string ServerName { get; set; }
    public string ServerAddress { get; set; }
    public string ServerKey { get; set; }
    public string AdminAddress { get; set; }

    public ServerOption ToServerOption()
    {
        return new ServerOption(ServerName, ServerAddress);
    }
}

public static class ReleaseServerConfigResolver
{
    private const string ReleaseVersionKey = "release_version";
    private const string ReleaseHostKey = "release_host";
    private const int BaseHttpPort = 7350;
    private const int PortStep = 10;

    public static ReleaseServerConfig ResolveByMode(LaunchServerEntry selectedServer, CfgMgr cfgMgr, out string error)
    {
        error = null;
        if (selectedServer == null)
        {
            error = "No server is configured.";
            return null;
        }

        var modeName = (selectedServer.modeName ?? string.Empty).Trim();
        if (string.Equals(modeName, "local", StringComparison.OrdinalIgnoreCase))
        {
            var localHost = (selectedServer.serverAddress ?? string.Empty).Trim();
            var localServerKey = cfgMgr?.GetGlobalConfigValue("server_key").Trim() ?? string.Empty;
            var localVersion = cfgMgr?.GetGlobalConfigValue(ReleaseVersionKey).Trim() ?? string.Empty;

            if (!int.TryParse(localVersion, out var localReleaseNumber) || localReleaseNumber <= 0)
            {
                error = "Local version must be a positive integer.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(localHost))
            {
                error = "Local server address is invalid.";
                return null;
            }

            if (string.IsNullOrWhiteSpace(localServerKey))
            {
                error = "Missing global config key: server_key";
                return null;
            }

            return BuildConfig(localVersion, localReleaseNumber, localHost, localServerKey, "Local");
        }

        return ResolveReleaseEntry(selectedServer, cfgMgr, out error);
    }

    public static bool TryResolve(CfgMgr cfgMgr, out ReleaseServerConfig config, out string error)
    {
        config = null;
        error = null;

        if (cfgMgr == null || !cfgMgr.IsLoaded)
        {
            error = "Global config is not loaded.";
            return false;
        }

        var version = cfgMgr.GetGlobalConfigValue(ReleaseVersionKey).Trim();
        if (string.IsNullOrWhiteSpace(version))
        {
            error = $"Missing global config key: {ReleaseVersionKey}";
            return false;
        }

        if (!int.TryParse(version, out var releaseNumber) || releaseNumber <= 0)
        {
            error = "release_version must be a positive integer.";
            return false;
        }

        var releaseHost = cfgMgr.GetGlobalConfigValue(ReleaseHostKey).Trim();
        if (string.IsNullOrWhiteSpace(releaseHost))
        {
            error = $"Missing global config key: {ReleaseHostKey}";
            return false;
        }

        var serverKey = cfgMgr.GetGlobalConfigValue("server_key").Trim();

        if (string.IsNullOrWhiteSpace(serverKey))
        {
            error = "Missing global config key: server_key";
            return false;
        }

        config = BuildConfig(version, releaseNumber, releaseHost, serverKey, "Release");
        if (config == null)
        {
            error = "Failed to build release config.";
            return false;
        }
        return true;
    }

    public static string TryExtractGameIp(ReleaseServerConfig config)
    {
        if (config == null || !Uri.TryCreate(config.ServerAddress, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        return uri.Host ?? string.Empty;
    }

    public static ReleaseServerConfig TryResolveForEditor(CfgMgr cfgMgr)
    {
        return TryResolve(cfgMgr, out var config, out _) ? config : null;
    }

    public static ReleaseServerConfig ResolveReleaseEntry(LaunchServerEntry entry, CfgMgr cfgMgr, out string error)
    {
        error = null;

        if (entry == null)
        {
            error = "Release entry is null.";
            return null;
        }

        if (cfgMgr == null || !cfgMgr.IsLoaded)
        {
            error = "Global config is not loaded.";
            return null;
        }

        var version = cfgMgr.GetGlobalConfigValue(ReleaseVersionKey).Trim();
        if (string.IsNullOrWhiteSpace(version))
        {
            error = $"Missing global config key: {ReleaseVersionKey}";
            return null;
        }

        if (!int.TryParse(version, out var releaseNumber) || releaseNumber <= 0)
        {
            error = "release_version must be a positive integer.";
            return null;
        }

        var releaseHost = (entry.serverAddress ?? string.Empty).Trim();
        if (string.IsNullOrWhiteSpace(releaseHost))
        {
            releaseHost = cfgMgr.GetGlobalConfigValue(ReleaseHostKey).Trim();
        }

        if (string.IsNullOrWhiteSpace(releaseHost))
        {
            error = $"Missing global config key: {ReleaseHostKey}";
            return null;
        }

        var serverKey = cfgMgr.GetGlobalConfigValue("server_key").Trim();
        if (string.IsNullOrWhiteSpace(serverKey))
        {
            error = "Missing global config key: server_key";
            return null;
        }

        var config = BuildConfig(version, releaseNumber, releaseHost, serverKey, "Release");
        if (config == null)
        {
            error = "Failed to build release config.";
            return null;
        }

        return config;
    }

    public static int GetHttpPort(int releaseNumber)
    {
        return BaseHttpPort + ((releaseNumber - 1) * PortStep);
    }

    public static int GetGrpcPort(int releaseNumber)
    {
        return GetHttpPort(releaseNumber) - 1;
    }

    public static int GetAdminPort(int releaseNumber)
    {
        return GetHttpPort(releaseNumber) + 1;
    }

    public static string BuildServerName(string version, string prefix)
    {
        return $"{prefix} {version}";
    }

    private static string BuildHttpUrl(string baseUrl, int port)
    {
        if (!Uri.TryCreate(baseUrl, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        var builder = new UriBuilder(uri)
        {
            Port = port,
        };
        return builder.Uri.ToString().TrimEnd('/');
    }

    private static ReleaseServerConfig BuildConfig(string version, int releaseNumber, string baseUrl, string serverKey, string namePrefix)
    {
        var serverAddress = BuildHttpUrl(baseUrl, GetHttpPort(releaseNumber));
        var adminAddress = BuildHttpUrl(baseUrl, GetAdminPort(releaseNumber));

        if (!Uri.TryCreate(serverAddress, UriKind.Absolute, out _))
        {
            return null;
        }

        if (!Uri.TryCreate(adminAddress, UriKind.Absolute, out _))
        {
            return null;
        }

        return new ReleaseServerConfig
        {
            Version = version,
            ServerName = BuildServerName(version, namePrefix),
            ServerAddress = serverAddress,
            ServerKey = serverKey,
            AdminAddress = adminAddress,
        };
    }
}
