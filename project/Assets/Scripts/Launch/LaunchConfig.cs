using UnityEngine;

[CreateAssetMenu(fileName = "LaunchConfig", menuName = "Game/Launch Config")]
public sealed class LaunchConfig : ScriptableObject
{
    [SerializeField] private bool enableLogger = true;
    [SerializeField] private string userName = "player_001";
    [SerializeField] private LaunchServerMode selectedServerMode = LaunchServerMode.Local;
    [SerializeField] private int serverVersion = 101;
    [SerializeField] private string localHost = "http://127.0.0.1";
    [SerializeField] private string devHost = "http://127.0.0.1";
    [SerializeField] private string releaseHost = "http://127.0.0.1";
    [SerializeField] private string serverKey = "local_socket_server_key_change_me";

    public bool EnableLogger => enableLogger;
    public string UserName => userName;
    public LaunchServerMode SelectedServerMode => selectedServerMode;
    public int ServerVersion => serverVersion;
    public string LocalHost => localHost;
    public string DevHost => devHost;
    public string ReleaseHost => releaseHost;
    public string ServerKey => serverKey;
}
