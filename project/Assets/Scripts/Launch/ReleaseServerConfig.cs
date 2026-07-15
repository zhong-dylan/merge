using System;
using UnityEngine;

public enum LaunchServerMode
{
    Local,
    Dev,
    Release,
}

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

[Serializable]
public class ServerOption
{
    [SerializeField] private string displayName;
    [SerializeField] private string address;

    public string DisplayName => displayName;
    public string Address => address;

    public ServerOption(string displayName, string address)
    {
        this.displayName = displayName;
        this.address = address;
    }
}

public static class ReleaseServerConfigResolver
{
    private const string ReleaseVersionKey = "release_version";
    private const string LocalHostKey = "local_host";
    private const string DevHostKey = "dev_host";
    private const string ReleaseHostKey = "release_host";
    private const int BaseHttpPort = 7350;
    private const int PortStep = 10;

    public static ReleaseServerConfig ResolveByMode(LaunchServerMode mode, CfgMgr cfgMgr, out string error)
    {
        error = null;
        return ResolveByHostKey(GetHostKey(mode), GetModeName(mode), cfgMgr, out error);
    }

    public static bool TryResolve(CfgMgr cfgMgr, out ReleaseServerConfig config, out string error)
    {
        config = ResolveReleaseEntry(cfgMgr, out error);
        return config != null;
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

    public static ReleaseServerConfig ResolveReleaseEntry(CfgMgr cfgMgr, out string error)
    {
        return ResolveByHostKey(ReleaseHostKey, "Release", cfgMgr, out error);
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

    private static ReleaseServerConfig ResolveByHostKey(string hostKey, string namePrefix, CfgMgr cfgMgr, out string error)
    {
        error = null;

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

        var host = cfgMgr.GetGlobalConfigValue(hostKey).Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            error = $"Missing global config key: {hostKey}";
            return null;
        }

        var serverKey = cfgMgr.GetGlobalConfigValue("server_key").Trim();
        if (string.IsNullOrWhiteSpace(serverKey))
        {
            error = "Missing global config key: server_key";
            return null;
        }

        var config = BuildConfig(version, releaseNumber, host, serverKey, namePrefix);
        if (config == null)
        {
            error = $"Failed to build {namePrefix} config.";
            return null;
        }

        return config;
    }

    private static string GetHostKey(LaunchServerMode mode)
    {
        return mode switch
        {
            LaunchServerMode.Local => LocalHostKey,
            LaunchServerMode.Dev => DevHostKey,
            _ => ReleaseHostKey,
        };
    }

    private static string GetModeName(LaunchServerMode mode)
    {
        return mode switch
        {
            LaunchServerMode.Local => "Local",
            LaunchServerMode.Dev => "Dev",
            _ => "Release",
        };
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
