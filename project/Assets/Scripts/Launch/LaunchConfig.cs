using System;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "LaunchConfig", menuName = "Game/Launch Config")]
public class LaunchConfig : ScriptableObject
{
    [SerializeField] private string serverKey = "local_socket_server_key_change_me";
    [SerializeField] private string userName = "player_001";
    [SerializeField] private List<ServerOption> serverOptions = new()
    {
        new ServerOption("Local", "http://127.0.0.1:7350"),
        new ServerOption("LAN", "http://192.168.1.100:7350"),
        new ServerOption("Production", "https://your-game-server.example.com"),
    };
    [SerializeField] private int selectedServerIndex;

    public string ServerKey => serverKey;
    public string UserName => userName;
    public IReadOnlyList<ServerOption> ServerOptions => serverOptions;
    public int SelectedServerIndex => selectedServerIndex;

    public ServerOption SelectedServer
    {
        get
        {
            if (serverOptions == null || serverOptions.Count == 0)
            {
                return null;
            }

            var safeIndex = Mathf.Clamp(selectedServerIndex, 0, serverOptions.Count - 1);
            return serverOptions[safeIndex];
        }
    }

    public void SetSelectedServerIndex(int index)
    {
        if (serverOptions == null || serverOptions.Count == 0)
        {
            selectedServerIndex = 0;
            return;
        }

        selectedServerIndex = Mathf.Clamp(index, 0, serverOptions.Count - 1);
    }

    private void OnValidate()
    {
        SetSelectedServerIndex(selectedServerIndex);
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
