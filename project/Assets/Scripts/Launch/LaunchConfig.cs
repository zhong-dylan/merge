using UnityEngine;

[CreateAssetMenu(fileName = "LaunchConfig", menuName = "Game/Launch Config")]
public sealed class LaunchConfig : ScriptableObject
{
    [SerializeField] private bool enableLogger = true;
    [SerializeField] private int serverVersion = 101;
    [SerializeField] private string host = "http://127.0.0.1";
    [SerializeField] private string serverKey = "local_socket_server_key_change_me";

    public bool EnableLogger => enableLogger;
    public int ServerVersion => serverVersion;
    public string Host => host;
    public string ServerKey => serverKey;
}
