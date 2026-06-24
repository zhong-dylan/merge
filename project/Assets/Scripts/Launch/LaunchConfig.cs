using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LaunchConfig", menuName = "Game/Launch Config")]
public class LaunchConfig : ScriptableObject
{
    [SerializeField] private bool enableLogger = true;
    [SerializeField] private string userName = "player_001";
    [SerializeField] private List<LaunchServerEntry> servers = new()
    {
        new LaunchServerEntry
        {
            modeName = "local",
            serverAddress = "http://127.0.0.1",
        },
        new LaunchServerEntry
        {
            modeName = "dev",
            serverAddress = string.Empty,
        },
        new LaunchServerEntry
        {
            modeName = "release",
            serverAddress = string.Empty,
        },
    };

    public bool EnableLogger => enableLogger;
    public string UserName => userName;
    public IReadOnlyList<LaunchServerEntry> Servers => servers;

    private void OnValidate()
    {
        if (servers == null || servers.Count == 0)
        {
            servers = new List<LaunchServerEntry>
            {
                new LaunchServerEntry
                {
                    modeName = "local",
                    serverAddress = "http://127.0.0.1",
                },
            };
        }
    }
}

[Serializable]
public class LaunchServerEntry
{
    public string modeName;
    public string serverAddress;
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
