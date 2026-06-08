using System;
using Nakama;
using UnityEngine;

public class GameLaunch : MonoBehaviour
{
    [SerializeField] private LaunchConfig launchConfig;

    private void Start()
    {
        if (launchConfig == null)
        {
            Debug.LogWarning("LaunchConfig is not assigned.");
            return;
        }

        var selectedServer = launchConfig.SelectedServer;
        if (selectedServer == null)
        {
            Debug.LogWarning("LaunchConfig does not contain any server options.");
            return;
        }

        StartCoroutine(LoginCoroutine(selectedServer));
    }

    private System.Collections.IEnumerator LoginCoroutine(ServerOption selectedServer)
    {
        var username = launchConfig.UserName.Trim();
        if (string.IsNullOrWhiteSpace(username))
        {
            Debug.LogError("Username cannot be empty.");
            yield break;
        }

        var customId = BuildCustomId(username);
        var serverUri = new Uri(selectedServer.Address);
        var client = new Client(serverUri.Scheme, serverUri.Host, serverUri.Port, launchConfig.ServerKey);
        client.Timeout = 10;

        Debug.Log($"Launching with user '{username}' on server '{selectedServer.DisplayName}' at {selectedServer.Address}");

        var authenticateTask = client.AuthenticateCustomAsync(customId, username, true);
        yield return new WaitUntil(() => authenticateTask.IsCompleted);

        if (authenticateTask.IsFaulted)
        {
            Debug.LogError($"Nakama login failed: {authenticateTask.Exception}");
            yield break;
        }

        if (authenticateTask.IsCanceled)
        {
            Debug.LogError("Nakama login was canceled.");
            yield break;
        }

        var rpcTask = client.RpcAsync(authenticateTask.Result, "get_player_profile", "{}");
        yield return new WaitUntil(() => rpcTask.IsCompleted);

        if (rpcTask.IsFaulted)
        {
            Debug.LogError($"Nakama bootstrap RPC failed: {rpcTask.Exception}");
            yield break;
        }

        if (rpcTask.IsCanceled)
        {
            Debug.LogError("Nakama bootstrap RPC was canceled.");
            yield break;
        }

        var profile = JsonUtility.FromJson<PlayerBootstrapResponse>(rpcTask.Result.Payload);

        if (profile == null || string.IsNullOrWhiteSpace(profile.userId))
        {
            Debug.LogError("Invalid bootstrap response from server.");
            yield break;
        }

        Debug.Log($"Login success. Username={profile.username}, UserID={profile.userId}, PlayerID={profile.playerId}, Gold={profile.gold}, Energy={profile.energy}, Diamond={profile.diamond}");
    }

    private static string BuildCustomId(string username)
    {
        var chars = username.Trim().ToLowerInvariant().ToCharArray();
        for (var i = 0; i < chars.Length; i++)
        {
            var isAlpha = chars[i] >= 'a' && chars[i] <= 'z';
            var isNumber = chars[i] >= '0' && chars[i] <= '9';
            if (!isAlpha && !isNumber && chars[i] != '_' && chars[i] != '-')
            {
                chars[i] = '-';
            }
        }

        var sanitized = new string(chars).Trim('-');
        if (sanitized.Length < 3)
        {
            throw new InvalidOperationException("Username must contain at least 3 valid characters.");
        }

        return $"user-{sanitized}";
    }
}
