using System;
using UnityEngine;

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
    private const int BaseHttpPort = 7350;
    private const int PortStep = 10;

    public static string TryExtractGameIp(ReleaseServerConfig config)
    {
        if (config == null || !Uri.TryCreate(config.ServerAddress, UriKind.Absolute, out var uri))
        {
            return string.Empty;
        }

        return uri.Host ?? string.Empty;
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

    public static ReleaseServerConfig ResolveByHost(string host, string namePrefix, int releaseNumber, string serverKey, out string error)
    {
        error = null;

        if (releaseNumber <= 0)
        {
            error = "Server version must be a positive integer.";
            return null;
        }

        host = host?.Trim();
        if (string.IsNullOrWhiteSpace(host))
        {
            error = $"{namePrefix} host is empty.";
            return null;
        }

        serverKey = serverKey?.Trim();
        if (string.IsNullOrWhiteSpace(serverKey))
        {
            error = "Server key is empty.";
            return null;
        }

        var version = releaseNumber.ToString();
        var config = BuildConfig(version, releaseNumber, host, serverKey, namePrefix);
        if (config == null)
        {
            error = $"Failed to build {namePrefix} config.";
            return null;
        }

        return config;
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
