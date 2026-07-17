using System;
using System.Collections;
using Nakama;
using UnityEngine;

public sealed class NakamaModel : ModelBase<NakamaModel>, IModel
{
    public Client Client { get; private set; }
    public ISession Session { get; private set; }
    public string ReleaseVersion { get; private set; }
    public string ServerName { get; private set; }
    public string ServerAddress { get; private set; }
    public string ServerKey { get; private set; }
    public string AdminAddress { get; private set; }

    public override void Init()
    {
        Clear();
    }

    public void SetServer(string releaseVersion, string serverName, string serverAddress, string serverKey, string adminAddress)
    {
        ReleaseVersion = releaseVersion ?? string.Empty;
        ServerName = serverName ?? string.Empty;
        ServerAddress = serverAddress ?? string.Empty;
        ServerKey = serverKey ?? string.Empty;
        AdminAddress = adminAddress ?? string.Empty;
    }

    public void SetConnection(Client client, ISession session)
    {
        Client = client;
        Session = session;
    }

    public IEnumerator Login(ServerOption selectedServer, string userName, Action<PlayerBootstrapResponse> onSuccess, Action<string> onFailed)
    {
        if (selectedServer == null)
        {
            onFailed?.Invoke("Server option is null.");
            yield break;
        }

        var username = userName?.Trim() ?? string.Empty;
        if (string.IsNullOrWhiteSpace(username))
        {
            onFailed?.Invoke("Username cannot be empty.");
            yield break;
        }

        var customId = BuildCustomId(username);
        var serverUri = new Uri(selectedServer.Address);
        var client = new Client(serverUri.Scheme, serverUri.Host, serverUri.Port, ServerKey);
        client.Timeout = 10;

        Logger.Info($"Launching with user '{username}' on server '{selectedServer.DisplayName}' at {selectedServer.Address}");

        var authenticateTask = client.AuthenticateCustomAsync(customId, username, true);
        yield return new WaitUntil(() => authenticateTask.IsCompleted);

        if (authenticateTask.IsFaulted)
        {
            onFailed?.Invoke(FormatNakamaFailure("Nakama login failed", authenticateTask.Exception));
            yield break;
        }

        if (authenticateTask.IsCanceled)
        {
            onFailed?.Invoke("Nakama login was canceled.");
            yield break;
        }

        SetConnection(client, authenticateTask.Result);

        var rpcTask = client.RpcAsync(authenticateTask.Result, "get_player_profile", "{}");
        yield return new WaitUntil(() => rpcTask.IsCompleted);

        if (rpcTask.IsFaulted)
        {
            SetConnection(null, null);
            onFailed?.Invoke(FormatNakamaFailure("Nakama bootstrap RPC failed", rpcTask.Exception));
            yield break;
        }

        if (rpcTask.IsCanceled)
        {
            SetConnection(null, null);
            onFailed?.Invoke("Nakama bootstrap RPC was canceled.");
            yield break;
        }

        var profile = JsonUtility.FromJson<PlayerBootstrapResponse>(rpcTask.Result.Payload);
        if (profile == null || string.IsNullOrWhiteSpace(profile.userId))
        {
            onFailed?.Invoke("Invalid bootstrap response from server.");
            yield break;
        }

        UserDataModel.I.Apply(profile);
        Logger.Success($"Login success. Username={profile.username}, UserID={profile.userId}, PlayerID={profile.playerId}, Gold={profile.gold}, Energy={profile.energy}, Diamond={profile.diamond}");
        onSuccess?.Invoke(profile);
    }

    public void Clear()
    {
        Client = null;
        Session = null;
        ReleaseVersion = string.Empty;
        ServerName = string.Empty;
        ServerAddress = string.Empty;
        ServerKey = string.Empty;
        AdminAddress = string.Empty;
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

    private string FormatNakamaFailure(string prefix, Exception exception)
    {
        if (TryGetApiResponseException(exception, out var apiResponseException)
            && apiResponseException.GrpcStatusCode == ErrorCode.ServerVersionDowngradeForbidden)
        {
            return $"账号已进入过更高版本服务器，禁止回到低版本服务器。当前尝试连接版本={ReleaseVersion}, Server={ServerName}, Address={ServerAddress}, ErrorCode={apiResponseException.GrpcStatusCode}";
        }

        var detail = ExtractExceptionMessage(exception);
        return string.IsNullOrWhiteSpace(detail)
            ? $"{prefix}: unknown error."
            : $"{prefix}: {detail}";
    }

    private static bool TryGetApiResponseException(Exception exception, out ApiResponseException apiResponseException)
    {
        apiResponseException = null;
        if (exception == null)
        {
            return false;
        }

        if (exception is ApiResponseException currentApiResponseException)
        {
            apiResponseException = currentApiResponseException;
            return true;
        }

        if (exception is AggregateException aggregateException)
        {
            aggregateException = aggregateException.Flatten();
            for (var i = 0; i < aggregateException.InnerExceptions.Count; i++)
            {
                if (TryGetApiResponseException(aggregateException.InnerExceptions[i], out apiResponseException))
                {
                    return true;
                }
            }
        }

        if (exception.InnerException != null)
        {
            return TryGetApiResponseException(exception.InnerException, out apiResponseException);
        }

        return false;
    }

    private static string ExtractExceptionMessage(Exception exception)
    {
        if (exception == null)
        {
            return string.Empty;
        }

        if (exception is AggregateException aggregateException)
        {
            aggregateException = aggregateException.Flatten();
            for (var i = 0; i < aggregateException.InnerExceptions.Count; i++)
            {
                var message = ExtractExceptionMessage(aggregateException.InnerExceptions[i]);
                if (!string.IsNullOrWhiteSpace(message))
                {
                    return message;
                }
            }
        }

        if (!string.IsNullOrWhiteSpace(exception.Message))
        {
            return exception.Message;
        }

        return exception.ToString();
    }
}
