using UnityEngine;

public static class Logger
{
    private const string DefaultColor = "#FFFFFF";
    private const string WarnColor = "#F5C542";
    private const string ErrorColor = "#FF5C5C";
    private const string SuccessColor = "#58D68D";
    private const string InfoColor = "#5DADE2";

    public static bool EnableLog { get; private set; } = true;

    public static void SetEnable(bool enable)
    {
        EnableLog = enable;
    }

    public static void Log(object message, Object context = null)
    {
        if (!EnableLog)
        {
            return;
        }

        var content = WrapColor(message, DefaultColor);

        if (context == null)
        {
            Debug.Log(content);
            return;
        }

        Debug.Log(content, context);
    }

    public static void Warn(object message, Object context = null)
    {
        if (!EnableLog)
        {
            return;
        }

        var content = WrapColor(message, WarnColor);

        if (context == null)
        {
            Debug.LogWarning(content);
            return;
        }

        Debug.LogWarning(content, context);
    }

    public static void Error(object message, Object context = null)
    {
        var content = WrapColor(message, ErrorColor);

        if (context == null)
        {
            Debug.LogError(content);
            return;
        }

        Debug.LogError(content, context);
    }

    public static void Success(object message, Object context = null)
    {
        if (!EnableLog)
        {
            return;
        }

        PrintColored(message, SuccessColor, context);
    }

    public static void Info(object message, Object context = null)
    {
        if (!EnableLog)
        {
            return;
        }

        PrintColored(message, InfoColor, context);
    }

    public static void Color(object message, string htmlColor, Object context = null)
    {
        if (!EnableLog)
        {
            return;
        }

        PrintColored(message, htmlColor, context);
    }

    private static void PrintColored(object message, string htmlColor, Object context)
    {
        var content = WrapColor(message, htmlColor);
        if (context == null)
        {
            Debug.Log(content);
            return;
        }

        Debug.Log(content, context);
    }

    private static string WrapColor(object message, string htmlColor)
    {
        return $"<color={htmlColor}>{message}</color>";
    }
}
